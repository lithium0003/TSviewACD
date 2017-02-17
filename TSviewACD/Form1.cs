using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;
            treeView1.Sorted = true;
            InitializeListView();
            Config.Log.LogOut("Application Start.");
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            listView1.Sort();
        }

        private void LoadImage()
        {
            Win32.SHSTOCKICONINFO sii = new Win32.SHSTOCKICONINFO();
            sii.cbSize = Marshal.SizeOf(sii);
            Win32.SHGetStockIconInfo(Win32.SIID_FOLDER, Win32.SHGSI_ICON, ref sii);
            if (sii.hIcon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(sii.hIcon);
                imageList_icon.Images.Add("Folder", aIcon);
                imageList_small.Images.Add("Folder", aIcon);
            }

            Win32.IImageList imglist = null;
            int rsult = Win32.SHGetImageList(Win32.SHIL_EXTRALARGE, ref Win32.IID_IImageList, out imglist);

            IntPtr hicon = IntPtr.Zero;
            imglist.GetIcon((int)Win32.SIID_FOLDER, (int)Win32.ImageListDrawItemConstants.ILD_TRANSPARENT, ref hicon);
            if (hicon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(hicon);
                imageList_Large.Images.Add("Folder", aIcon);
            }

            Win32.SHGetStockIconInfo(Win32.SIID_STUFFEDFOLDER, Win32.SHGSI_ICON, ref sii);
            if (sii.hIcon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(sii.hIcon);
                imageList_icon.Images.Add("Folder2", aIcon);
                imageList_small.Images.Add("Folder2", aIcon);
            }
            imglist.GetIcon((int)Win32.SIID_STUFFEDFOLDER, (int)Win32.ImageListDrawItemConstants.ILD_TRANSPARENT, ref hicon);
            if (hicon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(hicon);
                imageList_Large.Images.Add("Folder2", aIcon);
            }

            Win32.SHGetStockIconInfo(Win32.SIID_DOCNOASSOC, Win32.SHGSI_ICON, ref sii);
            if (sii.hIcon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(sii.hIcon);
                imageList_icon.Images.Add("Doc", aIcon);
                imageList_small.Images.Add("Doc", aIcon);
            }
            imglist.GetIcon((int)Win32.SIID_DOCNOASSOC, (int)Win32.ImageListDrawItemConstants.ILD_TRANSPARENT, ref hicon);
            if (hicon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(hicon);
                imageList_Large.Images.Add("Doc", aIcon);
            }
            treeView1.ImageList = imageList_icon;
            listView1.SmallImageList = imageList_small;
            listView1.LargeImageList = imageList_Large;
        }

        private readonly SynchronizationContext synchronizationContext;
        private ListViewColumnSorter lvwColumnSorter;
        bool initialized = false;
        AmazonDrive Drive = new AmazonDrive();
        ListViewItem[] root_items;
        ListViewItem root_entry;
        string root_id;
        bool supressListviewRefresh = false;
        ListViewItem[] all_items;
        private int CriticalCount = 0;

        private async Task Login()
        {
            Config.Log.LogOut("Login Start.");
            try
            {
                // Login & GetEndpoint
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Login ...";
                if (await Drive.Login() &&
                    await Drive.GetEndpoint())
                {
                    initialized = true;
                    loginToolStripMenuItem.Enabled = false;
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Login done.";
                }
                else
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Login failed.";
                    return;
                }
                await InitView();
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
        }

        private async Task InitView()
        {
            try
            {
                // Load Root
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading Root...";
                var rootdata = await Drive.ListMetadata("isRoot:true");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "RootNode Loaded.";
                root_id = rootdata.data[0].id;

                // add tree Root
                // Load Children
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading Children...";
                var children = await Drive.ListChildren(rootdata.data[0].id);
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Children Loaded.";

                var childNodes = Task.WhenAll(children.data.Select(async x =>
                {
                    int img = (x.kind == "FOLDER") ? 0 : 2;
                    var node = new TreeNode(x.name, img, img);
                    node.Name = x.name;
                    node.Tag = x;
                    if (x.kind == "FOLDER")
                    {
                        // Load Grandchildren
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                        toolStripStatusLabel1.Text = "Loading Grandchildren...";
                        var grandchildren = await Drive.ListChildren(x.id);
                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripStatusLabel1.Text = "Grandchildren Loaded.";
                        var grandchildNodes = grandchildren.data.Select(y =>
                        {
                            int img2 = (y.kind == "FOLDER") ? 0 : 2;
                            var cnode = new TreeNode(y.name, img2, img2);
                            cnode.Name = y.name;
                            cnode.Tag = y;
                            return cnode;
                        });
                        node.Nodes.AddRange(grandchildNodes.ToArray());
                    }
                    return node;
                }));
                treeView1.BeginUpdate();
                try
                {
                    treeView1.Nodes.Clear();
                    treeView1.Nodes.AddRange(await childNodes);
                }
                finally
                {
                    treeView1.EndUpdate();
                }

                // add listview Root
                listView1.BeginUpdate();
                try
                {
                    listView1.Items.Clear();
                    var rootitem = new ListViewItem(
                        new string[] {
                            ".",
                            "",
                            rootdata.data[0].modifiedDate.ToString(),
                            rootdata.data[0].createdDate.ToString(),
                            "/",
                            rootdata.data[0].id,
                            "",
                        }, 0);
                    rootitem.Name = "/";
                    root_entry = new ListViewItem(
                        new string[] {
                            "..",
                            "",
                            rootdata.data[0].modifiedDate.ToString(),
                            rootdata.data[0].createdDate.ToString(),
                            "/",
                            rootdata.data[0].id,
                            ""
                        }, 0);
                    root_entry.Name = "/";
                    listView1.Items.Add(rootitem);
                    listView1.Items.Add(root_entry);
                    var childitem = children.data.Select(x =>
                    {
                        var item = new ListViewItem(
                            new string[] {
                            x.name,
                            x.contentProperties?.size?.ToString("#,0"),
                            x.modifiedDate.ToString(),
                            x.createdDate.ToString(),
                            "/"+treeView1.Nodes.OfType<TreeNode>().First(y => y.Tag == x).FullPath,
                            x.id,
                            x.contentProperties?.md5,
                            }, (x.kind == "FOLDER") ? 0 : 2);
                        item.Name = x.name;
                        item.Tag = treeView1.Nodes.OfType<TreeNode>().First(y => y.Tag == x);
                        return item;
                    });
                    root_items = (new ListViewItem[] { rootitem, root_entry }).Concat(childitem).ToArray();
                    listView1.Items.AddRange(childitem.ToArray());
                    listView1.Sort();
                }
                finally
                {
                    listView1.EndUpdate();
                }
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
        }

        private void InitializeListView()
        {
            // ListViewコントロールのプロパティを設定
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.Sorting = SortOrder.Ascending;

            // 列（コラム）ヘッダの作成
            listView1.Columns.Add("Name",200);
            listView1.Columns.Add("Size",90);
            listView1.Columns.Add("modifiedDate",120);
            listView1.Columns.Add("createdDate",120);
            listView1.Columns.Add("path",100);
            listView1.Columns.Add("id");
            listView1.Columns.Add("MD5");

            listView1.Columns[1].TextAlign = HorizontalAlignment.Right;

            lvwColumnSorter = new ListViewColumnSorter();
            lvwColumnSorter.ColumnModes = new ListViewColumnSorter.ComparerMode[]
            {
                ListViewColumnSorter.ComparerMode.String,
                ListViewColumnSorter.ComparerMode.Integer,
                ListViewColumnSorter.ComparerMode.DateTime,
                ListViewColumnSorter.ComparerMode.DateTime,
                ListViewColumnSorter.ComparerMode.String,
                ListViewColumnSorter.ComparerMode.String,
                ListViewColumnSorter.ComparerMode.String,
            };
            listView1.ListViewItemSorter = lvwColumnSorter;
            listView1.Sort();
        }

        private void ListViewLoad(TreeNode tree)
        {
            if (tree == null)
            {
                listView1.Items.Clear();
                listView1.Items.AddRange(root_items);
                listView1.Sort();
                return;
            }

            treeView1.SelectedNode = tree;
            treeView1.SelectedNode.Expand();
            var selectdata = tree.Tag as FileMetadata_Info;

            if (selectdata == null || selectdata.kind != "FOLDER") return;

            listView1.BeginUpdate();
            try
            {
                listView1.Items.Clear();
                var rootitem = new ListViewItem(
                    new string[] {
                    ".",
                    "",
                    selectdata.modifiedDate.ToString(),
                    selectdata.createdDate.ToString(),
                    "/"+tree.FullPath,
                    selectdata.id,
                    selectdata.contentProperties?.md5,
                    }, 0);
                rootitem.Name = string.IsNullOrEmpty(tree.FullPath) ? "/" : ".";
                rootitem.Tag = tree;
                listView1.Items.Add(rootitem);

                var parent = tree.Parent;
                if (parent != null)
                {
                    var updata = parent.Tag as FileMetadata_Info;
                    var upitem = new ListViewItem(
                        new string[] {
                            "..",
                            "",
                            updata.modifiedDate.ToString(),
                            updata.createdDate.ToString(),
                            "/"+parent.FullPath,
                            updata.id,
                            updata.contentProperties?.md5,
                        }, 0);
                    upitem.Name = string.IsNullOrEmpty(parent.FullPath) ? "/" : "..";
                    upitem.Tag = parent;
                    listView1.Items.Add(upitem);
                }
                else
                {
                    listView1.Items.Add(root_entry);
                }

                var childitem = tree.Nodes.OfType<TreeNode>().Select(x => new { data = x.Tag as FileMetadata_Info, tree = x }).Select(x =>
                {
                    var item = new ListViewItem(
                        new string[] {
                        x.data.name,
                        x.data.contentProperties?.size?.ToString("#,0"),
                        x.data.modifiedDate.ToString(),
                        x.data.createdDate.ToString(),
                        "/"+x.tree.FullPath,
                        x.data.id,
                        x.data.contentProperties?.md5,
                        }, (x.data.kind == "FOLDER") ? 0 : 2);
                    item.Name = x.data.name;
                    item.Tag = x.tree;
                    return item;
                });
                listView1.Items.AddRange(childitem.ToArray());
                listView1.Sort();
            }
            finally
            {
                listView1.EndUpdate();
            }
        }

        private async Task LoadTreeItem(TreeNode node)
        {
            try
            {
                var nodedata = node.Tag as FileMetadata_Info;

                if (nodedata.kind != "FOLDER") return;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading Children...";
                await Task.WhenAll(node.Nodes.OfType<TreeNode>().Select(async x =>
                {
                    var childnodedata = x.Tag as FileMetadata_Info;

                    if (childnodedata.kind != "FOLDER") return;
                    if (x.Nodes.Count > 0) return;

                    // Load Grandchildren
                    var children = await Drive.ListChildren(childnodedata.id);

                    var childNodes = children.data.Select(y =>
                    {
                        int img = (y.kind == "FOLDER") ? 0 : 2;
                        var cnode = new TreeNode(y.name, img, img);
                        cnode.Name = y.name;
                        cnode.Tag = y;
                        return cnode;
                    });
                    treeView1.BeginUpdate();
                    try
                    {
                        x.Nodes.Clear();
                        x.Nodes.AddRange(childNodes.ToArray());
                    }
                    finally
                    {
                        treeView1.EndUpdate();
                    }
                }));
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Children Loaded.";
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Children Load failed.";
            }
        }

        private void FollowPath(string path_str)
        {
            if (string.IsNullOrEmpty(path_str)) return;
            var path = path_str.Split('\\');
            TreeNodeCollection current = null;
            foreach (var p in path)
            {
                var find_result = (current ?? treeView1.Nodes).Find(p, false);
                if (find_result.Count() == 0) break;
                current = find_result[0].Nodes;
                treeView1.SelectedNode = find_result[0];
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            LoadImage();
            textBox_HostName.Text = Config.SendToHost;
            textBox_Port.Text = Config.SendToPort.ToString();
            textBox_SendPacketNum.Text = Config.SendPacketNum.ToString();
            textBox_SendDelay.Text = Config.SendDelay.ToString();
            textBox_SendLongOffset.Text = Config.SendLongOffset.ToString();
            textBox_SendRatebySendCount.Text = Config.SendRatebySendCount.ToString();
            textBox_SendRatebyTOTCount.Text = Config.SendRatebyTOTCount.ToString();
            textBox_VK.Text = Config.SendVK.ToString();
            textBox_keySendApp.Text = Config.SendVK_Application;
            await Login();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Drive.Cancel();
            if (CriticalCount > 0)
            {
                toolStripStatusLabel1.Text = "Critical operration is progress. Please retry.";
                e.Cancel = true;
            }
            else
            {
                Config.IsClosing = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Login();
        }

        private void textBox_HostName_TextChanged(object sender, EventArgs e)
        {
            Config.SendToHost = textBox_HostName.Text;
        }

        private void textBox_Port_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendToPort = int.Parse(textBox_Port.Text);
            }
            catch { }
        }

        private async void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            await LoadTreeItem(e.Node);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBox_path.Text = e.Node.FullPath;

            if (!supressListviewRefresh)
                ListViewLoad(e.Node);
        }

        private void largeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.LargeIcon;
            largeIconToolStripMenuItem.Checked = true;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
        }

        private void smallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.SmallIcon;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = true;
            listToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.List;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = true;
            tileToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Tile;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = true;
            detailToolStripMenuItem.Checked = false;
        }

        private void detailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = true;
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var selectitem = listView1.SelectedItems[0];

            selectitem.Selected = false;
            selectitem.Focused = false;
            ListViewLoad(selectitem.Tag as TreeNode);
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var selectitem = listView1.SelectedItems[0];

            var tree = selectitem.Tag as TreeNode;
            if (tree == null) return;

            supressListviewRefresh = true;
            try
            {
                treeView1.SelectedNode = tree;
                treeView1.SelectedNode.Expand();
            }
            finally
            {
                supressListviewRefresh = false;
            }

            textBox_path.Text = treeView1.SelectedNode.FullPath;
        }

        private void button_Go_Click(object sender, EventArgs e)
        {
            FollowPath(textBox_path.Text);
        }

        private async Task<int> DoFileUpload(IEnumerable<string> Filenames, string parent_id, int f_all, int f_cur)
        {
            var ct = Drive.ct;
            FileMetadata_Info[] done_files = null;

            if (checkBox_upSkip.Checked)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Check Drive files...";
                var ret = await Drive.ListChildren(parent_id);
                done_files = ret.data;

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Check done.";
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripProgressBar1.Maximum = 100;
            }

            foreach (var filename in Filenames)
            {
                ct.ThrowIfCancellationRequested();
                Config.Log.LogOut("Upload File: "+filename);
                var upload_str = (f_all > 1) ? string.Format("Upload({0}/{1})...", ++f_cur, f_all) : "Upload...";
                var short_filename = System.IO.Path.GetFileName(filename);

                if(done_files?.Select(x => x.name).Contains(short_filename)??false)
                {
                    var target = done_files.First(x => x.name == short_filename);
                    if (new System.IO.FileInfo(filename).Length == target.contentProperties?.size)
                    {
                        if (!checkBox_MD5.Checked)
                            continue;
                        using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                        using (var hfile = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] md5 = null;
                            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                            toolStripStatusLabel1.Text = "Check file MD5...";
                            await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, Drive.ct);
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripStatusLabel1.Text = "Check done.";
                            if (BitConverter.ToString(md5).ToLower().Replace("-", "") == target.contentProperties?.md5)
                                continue;
                        }
                    }
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = upload_str + " " + short_filename;
                toolStripProgressBar1.Maximum = 10000;

                int retry = 6;
                while (--retry > 0)
                {
                    int checkretry = 4;
                    try
                    {
                        var ret = await Drive.uploadFile(
                            filename,
                            parent_id,
                            (src, evnt) =>
                            {
                                synchronizationContext.Post(
                                    (o) =>
                                    {
                                        if (ct.IsCancellationRequested) return;
                                        var eo = o as PositionChangeEventArgs;
                                        toolStripStatusLabel1.Text = upload_str + eo.Log + " " + short_filename;
                                        toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                    }, evnt);
                            });
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.Message.Contains("408 (REQUEST_TIMEOUT)")) checkretry = 6 * 5 + 1;
                        if (ex.Message.Contains("409 (Conflict)")) checkretry = 6 * 5 + 1;
                        if (ex.Message.Contains("504 (GATEWAY_TIMEOUT)")) checkretry = 6 * 5 + 1;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        checkretry = 3 + 1;
                    }

                    Config.Log.LogOut("Upload faild."+retry.ToString());
                    // wait for retry
                    while (--checkretry > 0)
                    {
                        try
                        {
                            Config.Log.LogOut("Upload : wait 10sec for retry..." + checkretry.ToString());
                            await Task.Delay(TimeSpan.FromSeconds(10), ct);

                            var children = await Drive.ListChildren(parent_id);
                            if (children.data.Select(x => x.name).Contains(short_filename))
                            {
                                Config.Log.LogOut("Upload : child found.");
                                break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (checkretry > 0)
                        break;
                }
                if (retry == 0)
                {
                    Config.Log.LogOut("Upload : failed.");
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 100;
                    toolStripStatusLabel1.Text = "Upload Failed.";
                    return -1;
                }

                Config.Log.LogOut("Upload : done.");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Upload done.";
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripProgressBar1.Maximum = 100;
            }
            return f_cur;
        }

        private async Task<int> DoDirectoryUpload(IEnumerable<string> Filenames, string parent_id, int f_all, int f_cur)
        {
            var ct = Drive.ct;
            foreach (var filename in Filenames)
            {
                ct.ThrowIfCancellationRequested();
                var short_name = Path.GetFullPath(filename).Split(new char[]{ '\\','/' }).Last();

                // make subdirectory
                var newdir = await Drive.createFolder(short_name, parent_id);

                f_cur = await DoFileUpload(Directory.EnumerateFiles(filename), newdir.id, f_all, f_cur);
                if (f_cur < 0) return -1;

                f_cur = await DoDirectoryUpload(Directory.EnumerateDirectories(filename), newdir.id, f_all, f_cur);
                if (f_cur < 0) return -1;
            }
            return f_cur;
        }

        private async void button_upload_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Upload Start.");
            toolStripStatusLabel1.Text = "unable to upload.";
            if (!initialized) return;
            toolStripStatusLabel1.Text = "Upload...";
            openFileDialog1.Title = "Select Upload File(s)";
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                toolStripStatusLabel1.Text = "Canceled.";
                return;
            }

            all_items = null;
            try
            {
                int f_all = openFileDialog1.FileNames.Count();
                int f_cur = 0;
                string parent_id = null;
                TreeNode cur_tree = null;
                string path = "";
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    if (currect.Length == 0) return;
                    cur_tree = (currect[0].Tag as TreeNode);
                    path = cur_tree.FullPath;
                    parent_id = (cur_tree.Tag as FileMetadata_Info).id;
                }
                catch { }

                if (await DoFileUpload(openFileDialog1.FileNames, parent_id, f_all, f_cur) < 0) return;

                if (cur_tree != null)
                    cur_tree.Nodes.Clear();

                if (cur_tree == null || cur_tree.Parent == null)
                    await InitView();
                else
                    await LoadTreeItem(cur_tree.Parent);
                FollowPath(path);
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 100;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
        }

        private async void trashItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Trash Start.");
            toolStripStatusLabel1.Text = "unable to trash.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            if (MessageBox.Show("Do you want to trash items?", "Trash Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            all_items = null;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items...";
                toolStripProgressBar1.Maximum = select.Count;
                toolStripProgressBar1.Step = 1;

                TreeNode cur_tree = null;
                string path = "";
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    cur_tree = (currect[0].Tag as TreeNode);
                    path = cur_tree.FullPath;
                }
                catch { }

                foreach (ListViewItem item in select)
                {
                    var ret = await Drive.TrashItem(((item.Tag as TreeNode).Tag as FileMetadata_Info).id);
                    toolStripProgressBar1.PerformStep();
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;

                if (cur_tree != null)
                    cur_tree.Nodes.Clear();

                if (cur_tree == null || cur_tree.Parent == null)
                    await InitView();
                else
                    await LoadTreeItem(cur_tree.Parent);
                FollowPath(path);
                Config.Log.LogOut("Trash : done.");
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                Config.Log.LogOut("Trash : error.");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("Rename : ERROR\r\n" + ex.Message);
            }
        }

        private async void button_reload_Click(object sender, EventArgs e)
        {
            all_items = null;
            var currect = listView1.Items.Find(".", false);
            try
            {
                try
                {
                    var path = (currect[0].Tag as TreeNode).FullPath;

                    (currect[0].Tag as TreeNode).Nodes.Clear();
                    if ((currect[0].Tag as TreeNode).Parent == null)
                        await InitView();
                    else
                        await LoadTreeItem((currect[0].Tag as TreeNode).Parent);
                    FollowPath(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    await InitView();
                }
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
        }

        private void button_break_Click(object sender, EventArgs e)
        {
            Drive.Cancel();
        }

        private async void downloadItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Download Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as TreeNode).Tag as FileMetadata_Info).Where(x => x.kind != "FOLDER");

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            int f_cur = 0;
            string savefilename = null;
            string savefilepath = null;
            string error_log = "";
            try
            {
                var ct = Drive.ct;
                toolStripStatusLabel1.Text = "place to download selection.";
                if (f_all > 1)
                {
                    folderBrowserDialog1.Description = "Select Save Folder for Download Items";
                    if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
                    savefilepath = folderBrowserDialog1.SelectedPath;
                }
                else
                {
                    saveFileDialog1.FileName = selectItem.First().name;
                    saveFileDialog1.Title = "Select Save Fileneme for Download";
                    if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
                    savefilename = saveFileDialog1.FileName;
                }

                foreach (var downitem in selectItem)
                {
                    Config.Log.LogOut("Download : " + downitem.name);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = download_str + " " + downitem.name;
                    toolStripProgressBar1.Maximum = 10000;

                    if (savefilepath != null)
                        savefilename = System.IO.Path.Combine(savefilepath, downitem.name);
                    var retry = 5;
                    var strerr = "";
                    while (--retry > 0)
                        try
                        {
                            using (var outfile = System.IO.File.OpenWrite(savefilename))
                            {
                                if (downitem.contentProperties.size > 10 * 1024 * 1024 * 1024L)
                                {
                                    Config.Log.LogOut("Download : <BIG FILE> temporary filename change");
                                    Interlocked.Increment(ref CriticalCount);
                                    try
                                    {
                                        try
                                        {
                                            var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id);
                                            var ret = await Drive.downloadFile(downitem.id);
                                            var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                            f.PosChangeEvent += (src, evnt) =>
                                            {
                                                synchronizationContext.Post(
                                                    (o) =>
                                                    {
                                                        if (ct.IsCancellationRequested) return;
                                                        var eo = o as PositionChangeEventArgs;
                                                        toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                                        toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                                    }, evnt);
                                            };
                                            await f.CopyToAsync(outfile, 16 * 1024 * 1024, Drive.ct);
                                        }
                                        finally
                                        {
                                            await Drive.renameItem(downitem.id, downitem.name);
                                        }
                                    }
                                    finally
                                    {
                                        Interlocked.Decrement(ref CriticalCount);
                                    }
                                }
                                else
                                {
                                    var ret = await Drive.downloadFile(downitem.id);
                                    var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        synchronizationContext.Post(
                                            (o) =>
                                            {
                                                if (ct.IsCancellationRequested) return;
                                                var eo = o as PositionChangeEventArgs;
                                                toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                                toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                            }, evnt);
                                    };
                                    await f.CopyToAsync(outfile, 16 * 1024 * 1024, Drive.ct);
                                }
                            }
                            Config.Log.LogOut("Download : done.");
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("Download : Error");
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Error detected.";
                            strerr += ex + "\r\n";
                            continue;
                        }


                    error_log += (strerr != "") ? (downitem.name + "\r\n" + strerr) : "";
                    if (retry == 0)
                    {
                        MessageBox.Show("Download : ERROR\r\n" + error_log);
                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripStatusLabel1.Text = "Error detected.";
                        return;
                    }

                    toolStripStatusLabel1.Text = "download done.";
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Download Items done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            if (error_log != "")
            {
                MessageBox.Show("Download : WARNING\r\n" + error_log);
            }
        }

        private async void sendUDPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await PlayTSFiles();
        }

        private ListViewItem MakeListviewItem(FileMetadata_Info info, TreeNode tree)
        {
            var item = new ListViewItem(
                new string[] {
                        info.name,
                        info.contentProperties?.size?.ToString("#,0"),
                        info.modifiedDate.ToString(),
                        info.createdDate.ToString(),
                        "/"+tree.FullPath,
                        info.id,
                }, (info.kind == "FOLDER") ? 0 : 2);
            item.Name = info.name;
            item.Tag = tree;
            return item;
        }

        private async Task<List<ListViewItem>> DecendTree(TreeNode node)
        {
            List<ListViewItem> ret = new List<ListViewItem>();
            ret.Add(MakeListviewItem((node.Tag as FileMetadata_Info), node));

            if ((node.Tag as FileMetadata_Info).kind != "FOLDER")
            {
                return ret;
            }

            foreach (TreeNode child in node.Nodes)
            {
                await LoadTreeItem(node);
                ret.AddRange(await DecendTree(child));
            }
            return ret;
        }

        private async Task SearchAllDrive()
        {
            List<ListViewItem> ret = new List<ListViewItem>();
            foreach (TreeNode node in treeView1.Nodes)
            {
                await LoadTreeItem(node);
                ret.AddRange(await DecendTree(node));
            }
            all_items = ret.ToArray();
        }

        private async void button_search_Click(object sender, EventArgs e)
        {
            comboBox_FindStr.Items.Add(comboBox_FindStr.Text);

            if (all_items == null) await SearchAllDrive();

            ListViewItem[] selection = all_items;

            if (!checkBox_File.Checked && !checkBox_Folder.Checked)
            {
                //nothing
            }
            else
            {
                if (!checkBox_File.Checked)
                    selection = all_items.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).kind == "FOLDER").ToArray();
                if (!checkBox_Folder.Checked)
                    selection = all_items.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).kind != "FOLDER").ToArray();
            }

            if (checkBox_Regex.Checked)
                selection = selection.Where(x => Regex.IsMatch(((x.Tag as TreeNode).Tag as FileMetadata_Info).name, comboBox_FindStr.Text)).ToArray();
            else
                selection = selection.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).name.IndexOf(comboBox_FindStr.Text) >= 0).ToArray();

            if (radioButton_createTime.Checked)
            {
                if (checkBox_dateFrom.Checked)
                    selection = selection.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).createdDate > dateTimePicker_from.Value).ToArray();
                if (checkBox_dateTo.Checked)
                    selection = selection.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).createdDate < dateTimePicker_to.Value).ToArray();
            }
            if (radioButton_modifiedDate.Checked)
            {
                if (checkBox_dateFrom.Checked)
                    selection = selection.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).modifiedDate > dateTimePicker_from.Value).ToArray();
                if (checkBox_dateTo.Checked)
                    selection = selection.Where(x => ((x.Tag as TreeNode).Tag as FileMetadata_Info).modifiedDate < dateTimePicker_to.Value).ToArray();
            }

            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
            toolStripStatusLabel1.Text = "Found : " + selection.Length.ToString();

            listView1.Items.Clear();
            listView1.Items.AddRange(selection);
        }

        private async void button_mkdir_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("make Folder Start.");
            toolStripStatusLabel1.Text = "unable to mkFolder.";
            if (!initialized) return;
            toolStripStatusLabel1.Text = "mkFolder...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            all_items = null;
            try
            {
                string parent_id = null;
                TreeNode cur_tree = null;
                string path = "";
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    cur_tree = (currect[0].Tag as TreeNode);
                    path = cur_tree.FullPath;
                    parent_id = (cur_tree.Tag as FileMetadata_Info).id;
                }
                catch { }

                var newdir = await Drive.createFolder(textBox_newName.Text, parent_id);

                if (cur_tree != null)
                    cur_tree.Nodes.Clear();

                if (cur_tree == null || cur_tree.Parent == null)
                    await InitView();
                else
                    await LoadTreeItem(cur_tree.Parent);
                FollowPath(path);
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 100;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("mkFolder : ERROR\r\n" + ex.Message);
            }
        }

        private async void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("rename Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as TreeNode).Tag as FileMetadata_Info);

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            if (f_all > 1)
                if (MessageBox.Show("Do you want to rename multiple items?", "Rename Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            toolStripStatusLabel1.Text = "Rename...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            all_items = null;
            try
            {
                string parent_id = null;
                TreeNode cur_tree = null;
                string path = "";
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    cur_tree = (currect[0].Tag as TreeNode);
                    path = cur_tree.FullPath;
                    parent_id = (cur_tree.Tag as FileMetadata_Info).id;
                }
                catch { }

                foreach (var downitem in selectItem)
                {
                    using (var NewName = new FormInputName())
                    {
                        NewName.NewItemName = downitem.name;
                        if (NewName.ShowDialog() != DialogResult.OK) break;

                        var tmpfile = await Drive.renameItem(downitem.id, NewName.NewItemName);
                    }
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Rename Items done.";

                if (cur_tree != null)
                    cur_tree.Nodes.Clear();

                if (cur_tree == null || cur_tree.Parent == null)
                    await InitView();
                else
                    await LoadTreeItem(cur_tree.Parent);
                FollowPath(path);
                Config.Log.LogOut("rename : done.");
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                Config.Log.LogOut("rename : Error");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("Rename : ERROR\r\n" + ex.Message);
            }
        }

        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (listView1.SelectedItems.OfType<ListViewItem>().Count(x => x.Text == "." || x.Text == "..") > 0)
                return;
            listView1.DoDragDrop(listView1.SelectedItems, DragDropEffects.Move);
        }

        private void listView1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Point p = listView1.PointToClient(new Point(e.X, e.Y));
                ListViewItem item = listView1.GetItemAt(p.X, p.Y);
                var current = listView1.Items.Find(".", false);

                if (listView1.Items.IndexOf(item) < 0 || (item.Name != "/" && ((item?.Tag as TreeNode).Tag as FileMetadata_Info).kind != "FOLDER"))
                {
                    if (current.Length > 0) item = current[0];
                }

                if (item != null)
                {
                    if (item.Tag == null || item.Name == "/" || ((item.Tag as TreeNode).Tag as FileMetadata_Info).kind == "FOLDER")
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                            e.Effect = DragDropEffects.Copy;
                        else
                        {
                            if(item != ((current.Length > 0)? current[0]: null) &&
                                ((item.Name == "/" && ((current.Length > 0) ? current[0] : null).Name != "/") ||
                                !(((ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))?.Contains(item)?? false)))
                            {
                                e.Effect = DragDropEffects.Move;
                            }
                            else
                                e.Effect = DragDropEffects.None;
                        }
                    }
                    else
                        e.Effect = DragDropEffects.None;
                }
            }
        }

        private async void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    Point p = listView1.PointToClient(new Point(e.X, e.Y));
                    ListViewItem item = listView1.GetItemAt(p.X, p.Y);
                    if (listView1.Items.IndexOf(item) < 0 || (item.Name != "/" && ((item?.Tag as TreeNode).Tag as FileMetadata_Info).kind != "FOLDER"))
                    {
                        var current = listView1.Items.Find(".", false);
                        if (current.Length > 0) item = current[0];
                    }
                    if (item == null) return;

                    if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
                    {
                        Config.Log.LogOut("move(listview) Start.");
                        toolStripStatusLabel1.Text = "Move Item...";
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

                        string path = "";
                        try
                        {
                            var currect = listView1.Items.Find(".", false);
                            path = (currect[0].Tag as TreeNode).FullPath;
                        }
                        catch { }

                        all_items = null;
                        try
                        {
                            var toParent = (item.Tag == null) ? root_id : ((item.Tag as TreeNode).Tag as FileMetadata_Info).id;
                            foreach (ListViewItem aItem in (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))
                            {
                                var fromParent = ((aItem.Tag as TreeNode).Parent == null) ? root_id : ((aItem.Tag as TreeNode).Parent.Tag as FileMetadata_Info).id;
                                var childid = ((aItem.Tag as TreeNode).Tag as FileMetadata_Info).id;

                                await Drive.moveChild(childid, fromParent, toParent);
                            }

                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item done.";

                            await InitView();
                            FollowPath(path);
                            Config.Log.LogOut("move(listview) : done.");
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("move(listview) : Error");
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Error detected.";
                            MessageBox.Show("Move Item : ERROR\r\n" + ex.Message);
                        }
                    }
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        Config.Log.LogOut("upload(listview) Start.");
                        string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                        if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                            if (MessageBox.Show("Drag item contains some Folder. Do you want to continue?", "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

                        string[] dir_drags = drags.Where(x => Directory.Exists(x)).ToArray();
                        drags = drags.Where(x => File.Exists(x)).ToArray();

                        var ct = Drive.ct;
                        int f_all = drags.Length + dir_drags.Select(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).SelectMany(i => i).Distinct().Count();
                        int f_cur = 0;
                        string parent_id = null;
                        TreeNode cur_tree = null;
                        string path = "";
                        try
                        {
                            cur_tree = (item.Tag as TreeNode);
                            path = cur_tree.FullPath;
                            parent_id = (cur_tree.Tag as FileMetadata_Info).id;
                        }
                        catch { }

                        try
                        {
                            f_cur = await DoFileUpload(drags, parent_id, f_all, f_cur);
                            if (f_cur >= 0)
                                f_cur = await DoDirectoryUpload(dir_drags, parent_id, f_all, f_cur);

                            if (cur_tree != null)
                                cur_tree.Nodes.Clear();

                            if (cur_tree == null || cur_tree.Parent == null)
                                await InitView();
                            else
                                await LoadTreeItem(cur_tree.Parent);
                            FollowPath(path);
                            Config.Log.LogOut("upload(listview) : done.");
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("upload(listview) : Error");
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Error detected.";
                            MessageBox.Show("Upload Items : ERROR\r\n" + ex.Message);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!Config.IsClosing)
                    {
                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripProgressBar1.Maximum = 100;
                        toolStripStatusLabel1.Text = "Operation Aborted.";
                    }
                }
            }
        }

        TreeNode HoldonNode;

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Enabled = false;
            var pos = treeView1.PointToClient(Cursor.Position);
            TreeNode item = treeView1.GetNodeAt(pos.X, pos.Y);

            if (item == null) return;

            if(HoldonNode != item)
            {
                HoldonNode = null;
                return;
            }

            supressListviewRefresh = true;
            try
            {
                var children_kind = item.Nodes.OfType<TreeNode>().Select(x => (x.Tag as FileMetadata_Info).kind);
                if (children_kind.Where(x => x == "FOLDER").Count() > 0)
                {
                    // ノードを展開する。
                    item.Expand();
                }
            }
            finally
            {
                supressListviewRefresh = false;
            }
        }

        private void treeView1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Point p = treeView1.PointToClient(new Point(e.X, e.Y));
                TreeNode item = treeView1.GetNodeAt(p.X, p.Y);
                if (HoldonNode != item)
                    timer2.Enabled = false;
                HoldonNode = item;
                timer2.Enabled = true;

                if (p.Y < treeView1.Height / 2)
                {
                    item?.PrevNode?.EnsureVisible();
                    if (item?.PrevNode == null)
                        item?.Parent?.EnsureVisible();
                }
                else
                {
                    item?.NextNode?.EnsureVisible();
                }

                if (item == null || !string.IsNullOrEmpty((item.Tag as FileMetadata_Info).kind))
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.Move;

                        if (item != null)
                        {
                            while ((item.Tag as FileMetadata_Info).kind != "FOLDER")
                            {
                                item = item.Parent;
                                if (item == null) break;
                            }
                        }
                        var toParent = (item == null) ? root_id : (item.Tag as FileMetadata_Info).id;
                        foreach (ListViewItem aItem in (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))
                        {
                            var fromParent = ((aItem.Tag as TreeNode).Parent == null) ? root_id : ((aItem.Tag as TreeNode).Parent.Tag as FileMetadata_Info).id;
                            var childid = ((aItem.Tag as TreeNode).Tag as FileMetadata_Info).id;
                            if (toParent == fromParent || toParent == childid)
                            {
                                e.Effect = DragDropEffects.None;
                                break;
                            }
                        }
                    }
                }
                else
                    e.Effect = DragDropEffects.None;
            }
        }

        private async void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    Point p = treeView1.PointToClient(new Point(e.X, e.Y));
                    TreeNode item = treeView1.GetNodeAt(p.X, p.Y);

                    if (item != null)
                    {
                        while ((item.Tag as FileMetadata_Info).kind != "FOLDER")
                        {
                            item = item.Parent;
                            if (item == null) break;
                        }
                    }

                    string path = item?.FullPath;

                    if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
                    {
                        Config.Log.LogOut("move(treeview) Start.");
                        toolStripStatusLabel1.Text = "Move Item...";
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

                        all_items = null;
                        try
                        {
                            var toParent = (item == null)? root_id: (item.Tag as FileMetadata_Info).id;
                            foreach (ListViewItem aItem in (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))
                            {
                                var fromParent = ((aItem.Tag as TreeNode).Parent == null) ? root_id : ((aItem.Tag as TreeNode).Parent.Tag as FileMetadata_Info).id;
                                var childid = ((aItem.Tag as TreeNode).Tag as FileMetadata_Info).id;

                                await Drive.moveChild(childid, fromParent, toParent);
                            }

                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item done.";

                            await InitView();
                            FollowPath(path);
                            Config.Log.LogOut("move(treeview) : done.");
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("move(treeview) : Error");
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Error detected.";
                            MessageBox.Show("Move Item : ERROR\r\n" + ex.Message);
                        }
                    }
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        Config.Log.LogOut("upload(treeview) Start.");
                        string[] drags = (string[])e.Data.GetData(DataFormats.FileDrop);

                        if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                            if (MessageBox.Show("Drag item contains some Folder. Do you want to continue?", "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

                        string[] dir_drags = drags.Where(x => Directory.Exists(x)).ToArray();
                        drags = drags.Where(x => File.Exists(x)).ToArray();

                        int f_all = drags.Length + dir_drags.Select(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).SelectMany(i => i).Distinct().Count();
                        int f_cur = 0;
                        var parent_id = (item.Tag as FileMetadata_Info).id;

                        f_cur = await DoFileUpload(drags, parent_id, f_all, f_cur);
                        if (f_cur >= 0)
                            f_cur = await DoDirectoryUpload(dir_drags, parent_id, f_all, f_cur);

                        await InitView();
                        FollowPath(path);

                        Config.Log.LogOut("upload(treeview) : done.");
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!Config.IsClosing)
                    {
                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripProgressBar1.Maximum = 100;
                        toolStripStatusLabel1.Text = "Operation Aborted.";
                    }
                }
            }
        }

        private void logToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.Show(this);
        }

        private async void button_Play_Click(object sender, EventArgs e)
        {
            await PlayTSFiles();
        }

        private TimeSpan SendDuration;
        private TimeSpan SendStartDelay;
        private DateTime SendStartTime;

        private TimeSpan SeektoPos = TimeSpan.FromDays(100);
        CancellationTokenSource seek_ct_source = new CancellationTokenSource();

        private int nextcount = 0;

        private void CancelForSeek()
        {
            var t = seek_ct_source;
            seek_ct_source = new CancellationTokenSource();
            t.Cancel();
        }

        private async Task PlayOneTSFile(FileMetadata_Info downitem, string download_str)
        {
            long bytePerSec = 0;
            long? SkipByte = null;
            DateTime InitialTOT = default(DateTime);

            trackBar_Pos.Tag = 1;
            trackBar_Pos.Minimum = 0;
            trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / (10/8*1024*1024));
            trackBar_Pos.Value = 0;
            trackBar_Pos.Tag = 0;

            while (true)
            {
                PressKeyForOtherApp();

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = download_str + " " + downitem.name;
                toolStripProgressBar1.Maximum = 10000;

                var internalToken = seek_ct_source.Token;
                var externalToken = Drive.ct;
                try
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(internalToken, externalToken))
                    using (var ret = await Drive.downloadFile(downitem.id, SkipByte))
                    using (var bufst = new BufferedStream(ret, ConfigAPI.CopyBufferSize))
                    using (var f = new PositionStream(bufst, downitem.contentProperties.size.Value, SkipByte))
                    {
                        f.PosChangeEvent += (src, evnt) =>
                        {
                            synchronizationContext.Post(
                                (o) =>
                                {
                                    if (linkedCts.Token.IsCancellationRequested) return;
                                    var eo = o as PositionChangeEventArgs;
                                    toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                    toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                }, evnt);
                        };
                        using (var UDP = new UDP_TS_Stream(linkedCts.Token))
                        {
                            label_sendname.Text = downitem.name;
                            if (SeektoPos < TimeSpan.FromDays(30))
                            {
                                if(SendDuration != default(TimeSpan))
                                    UDP.SendDuration = SendDuration - SeektoPos;

                                if (InitialTOT != default(DateTime))
                                    UDP.SendStartTime = InitialTOT + SeektoPos;
                                else
                                    UDP.SendDelay = SeektoPos;
                            }
                            else
                            {
                                UDP.SendDuration = SendDuration;
                                if (SkipByte == null)
                                {
                                    UDP.SendDelay = SendStartDelay;
                                    UDP.SendStartTime = SendStartTime;
                                }
                                else
                                {
                                    if (SendStartTime != default(DateTime))
                                        UDP.SendStartTime = SendStartTime;
                                    else if (InitialTOT != default(DateTime))
                                        UDP.SendStartTime = InitialTOT + SendStartDelay;
                                }
                            }
                            UDP.TOTChangeHander += (src, evnt) =>
                            {
                                synchronizationContext.Post(
                                    (o) =>
                                    {
                                        //if (linkedCts.Token.IsCancellationRequested) return;
                                        var eo = o as TOTChangeEventArgs;
                                        if (InitialTOT == default(DateTime))
                                        {
                                            InitialTOT = (eo.initialTOT == default(DateTime))? eo.TOT_JST: eo.initialTOT;
                                        }
                                        bytePerSec = eo.bytePerSec;
                                        trackBar_Pos.Tag = 1;
                                        trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / eo.bytePerSec);
                                        trackBar_Pos.Value = (int)(((SkipByte??0) + eo.Position) / eo.bytePerSec);
                                        trackBar_Pos.Tag = 0;
                                        label_stream.Text = string.Format(
                                            "TOT:{0} pos {1} / {2} ({3} / {4})",
                                            eo.TOT_JST.ToString(),
                                            (eo.TOT_JST - InitialTOT).ToString(),
                                            TimeSpan.FromSeconds(downitem.contentProperties.size.Value / eo.bytePerSec).ToString(),
                                            (SkipByte ?? 0 + eo.Position).ToString("#,0"),
                                            downitem.contentProperties.size.Value.ToString("#,0"));
                                    }, evnt);
                            };
                            SeektoPos = TimeSpan.FromDays(100);
                            await f.CopyToAsync(UDP, ConfigAPI.CopyBufferSize, linkedCts.Token);
                        }
                    }
                    break;
                }
                catch (PlayEOF_CanceledException)
                {
                    break;
                }
                catch (SenderBreakCanceledException ex)
                {
                    bytePerSec = ex.bytePerSec;

                    if (SkipByte != null)
                        SkipByte += ex.WaitForByte;
                    else
                        SkipByte = ex.WaitForByte;

                    if (InitialTOT == default(DateTime))
                        InitialTOT = ex.InitialTOT;

                    trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / bytePerSec);

                    if (SkipByte > downitem.contentProperties.size)
                        break;
                    continue;
                }
                catch (OperationCanceledException)
                {
                    if (internalToken.IsCancellationRequested)
                    {
                        if (SeektoPos < TimeSpan.FromDays(30))
                        {
                            SkipByte = (long)(SeektoPos.TotalSeconds * bytePerSec * 0.9);
                            if (SkipByte > downitem.contentProperties.size)
                                break;
                            continue;
                        }
                        SeektoPos = TimeSpan.FromDays(100);
                        nextcount--;
                        break;
                    }
                    else if (externalToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    break;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        [DllImport("User32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        private void PressKeyForOtherApp()
        {
            try
            {
                var mainWindowHandle = System.Diagnostics.Process.GetProcessesByName(Config.SendVK_Application)[0].MainWindowHandle;
                PostMessage(mainWindowHandle, WM_KEYDOWN, (int)Config.SendVK, 0);
                PostMessage(mainWindowHandle, WM_KEYUP, (int)Config.SendVK, 0);
            }
            catch { }
        }

        private async Task PlayTSFiles()
        {
            Config.Log.LogOut("Send UDP TS stream Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as TreeNode).Tag as FileMetadata_Info).Where(x => x.kind != "FOLDER");

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            int f_cur = 0;
            try
            {
                nextcount = 0;
                foreach (var downitem in selectItem)
                {
                    Config.Log.LogOut("Send UDP download : " + downitem.name);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    if (downitem.contentProperties.size > ConfigAPI.FilenameChangeTrickSize)
                    {
                        Config.Log.LogOut("Send UDP download : <BIG FILE> temporary filename change");
                        Interlocked.Increment(ref CriticalCount);
                        try
                        {
                            toolStripStatusLabel1.Text = "temporary filename change...";
                            var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id);
                            try
                            {
                                await PlayOneTSFile(downitem, download_str);
                            }
                            finally
                            {
                                await Drive.renameItem(downitem.id, downitem.name);
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref CriticalCount);
                        }
                    }
                    else
                    {
                        await PlayOneTSFile(downitem, download_str);
                    }

                    Config.Log.LogOut("Send UDP download : done.");
                    toolStripStatusLabel1.Text = "download done.";
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Download Items done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                Config.Log.LogOut("Send UDP download : Error");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("sendUDP : ERROR\r\n" + ex.Message);
            }
            label_sendname.Text = "Send Filename";
        }

        private void textBox_Duration_Leave(object sender, EventArgs e)
        {
            if (textBox_Duration.Text == "")
                SendDuration = default(TimeSpan);
            else
            {
                try
                {
                    SendDuration = TimeSpan.FromSeconds(double.Parse(textBox_Duration.Text));
                }
                catch {
                    try
                    {
                        SendDuration = TimeSpan.Parse(textBox_Duration.Text);
                    }
                    catch {
                        SendDuration = default(TimeSpan);
                    }
                }
            }
            textBox_Duration.Text = (SendDuration == default(TimeSpan)) ? "" : SendDuration.ToString();
        }

        private void textBox_StartTime_Leave(object sender, EventArgs e)
        {
            if (radioButton_AbsTime.Checked)
            {
                SendStartDelay = default(TimeSpan);
                if (textBox_StartTime.Text == "")
                    SendStartTime = default(DateTime);
                else
                {
                    try
                    {
                        SendStartTime = DateTime.Parse(textBox_StartTime.Text);
                    }
                    catch
                    {
                        SendStartTime = default(DateTime);
                    }
                }
                textBox_StartTime.Text = (SendStartTime == default(DateTime)) ? "" : SendStartTime.ToString();
            }
            if (radioButton_DiffTime.Checked)
            {
                SendStartTime = default(DateTime);
                if (textBox_StartTime.Text == "")
                    SendStartDelay = default(TimeSpan);
                else
                {
                    try
                    {
                        SendStartDelay = TimeSpan.FromSeconds(double.Parse(textBox_StartTime.Text));
                    }
                    catch
                    {
                        try
                        {
                            SendStartDelay = TimeSpan.Parse(textBox_StartTime.Text);
                        }
                        catch
                        {
                            SendStartDelay = default(TimeSpan);
                        }
                    }
                }
                textBox_StartTime.Text = (SendStartDelay == default(TimeSpan)) ? "" : SendStartDelay.ToString();
            }
        }

        private void trackBar_Pos_ValueChanged(object sender, EventArgs e)
        {
            if (trackBar_Pos.Tag as int? == 1)
            {
                if (SeektoPos < TimeSpan.FromDays(30))
                {
                    trackBar_Pos.Tag = 1;
                    trackBar_Pos.Value = (int)SeektoPos.TotalSeconds;
                    trackBar_Pos.Tag = 0;
                }
            }
            else
            {
                timer1.Enabled = false;
                SeektoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
                label_stream.Text = string.Format(
                    "seeking to {0}",
                    SeektoPos.ToString());
                timer1.Enabled = true;
            }
        }

        private void trackBar_Pos_MouseCaptureChanged(object sender, EventArgs e)
        {
            SeektoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
            timer1.Enabled = false;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            SeektoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
            timer1.Enabled = false;
            CancelForSeek();
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            SeektoPos = TimeSpan.FromDays(100);
            nextcount++;
            CancelForSeek();
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                ProcessTabKey(true);
            }
        }

        private void textBox_SendPacketNum_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendPacketNum = int.Parse(textBox_SendPacketNum.Text);
            }
            catch {
                textBox_SendPacketNum.Text = Config.SendPacketNum.ToString();
            }
        }

        private void textBox_SendDelay_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendDelay = int.Parse(textBox_SendDelay.Text);
            }
            catch
            {
                textBox_SendDelay.Text = Config.SendDelay.ToString();
            }
        }

        private void textBox_SendLongOffset_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendLongOffset = int.Parse(textBox_SendLongOffset.Text);
            }
            catch
            {
                textBox_SendLongOffset.Text = Config.SendLongOffset.ToString();
            }
        }

        private void textBox_SendRatebySendCount_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendRatebySendCount = int.Parse(textBox_SendRatebySendCount.Text);
            }
            catch
            {
                textBox_SendRatebySendCount.Text = Config.SendRatebySendCount.ToString();
            }
        }

        private void textBox_SendRatebyTOTCount_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Config.SendRatebyTOTCount = int.Parse(textBox_SendRatebyTOTCount.Text);
            }
            catch
            {
                textBox_SendRatebyTOTCount.Text = Config.SendRatebyTOTCount.ToString();
            }
        }

        private void textBox_VK_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            textBox_VK.Text = e.KeyCode.ToString();
            Config.SendVK = e.KeyCode;
        }

        private void textBox_keySendApp_TextChanged(object sender, EventArgs e)
        {
            Config.SendVK_Application = textBox_keySendApp.Text;
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyData == (Keys.A | Keys.Control))
            {
                listView1.BeginUpdate();
                foreach(ListViewItem item in listView1.Items)
                {
                    if(item.Name != "." && item.Name != ".." && item.Name != "/")
                        item.Selected = true;
                }
                listView1.EndUpdate();
            }
        }

        private void logToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            logToFileToolStripMenuItem.Checked = !logToFileToolStripMenuItem.Checked;
            Config.LogToFile = logToFileToolStripMenuItem.Checked;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new AboutBox1()).ShowDialog();
        }

        private void button_LocalRemoteMatch_Click(object sender, EventArgs e)
        {
            var Matcher = new FormMatch();
            Matcher.SelectedRemoteFiles = 
                (listView1.SelectedItems.Count == 0? listView1.Items.OfType<ListViewItem>() : listView1.SelectedItems.OfType<ListViewItem>())
                .Where(item => (item.Name != "." && item.Name != ".." && item.Name != "/")).ToArray();
            Matcher.ShowDialog();
        }

        private void comboBox_FindStr_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                button_search.PerformClick();
            }
        }
    }
}

