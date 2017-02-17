using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace TSviewACD
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            toolStripMenuItem_Logout.Enabled = false;
            synchronizationContext = SynchronizationContext.Current;
            treeView1.Sorted = true;
            InitializeListView();
            Config.Log.LogOut("Application Start.");
        }

        private TaskCanselToken CreateTask(string taskname)
        {
            var item = new ListViewItem(taskname);
            var task = new TaskCanselToken(taskname);
            item.Tag = task;
            listView_TaskList.Items.Add(item);
            return task;
        }

        private void FinishTask(TaskCanselToken task)
        {
            var removes = listView_TaskList.Items.OfType<ListViewItem>().Where(x => x.Tag == task);
            if (removes.Count() > 0)
            {
                foreach (var item in removes)
                    listView_TaskList.Items.Remove(item);
            }
        }

        private async Task CancelTask(string taskname)
        {
            foreach (var item in listView_TaskList.Items.OfType<ListViewItem>().Where(x => (x.Tag as TaskCanselToken).taskname == taskname).ToArray())
            {
                (item.Tag as TaskCanselToken).cts.Cancel();
            }
            while (listView_TaskList.Items.OfType<ListViewItem>().Where(x => (x.Tag as TaskCanselToken).taskname == taskname).Count() > 0)
            {
                await Task.Delay(100);
            }
        }

        private bool CancelTaskAll()
        {
            var removes = listView_TaskList.Items;
            if (removes.Count > 0)
            {
                foreach (ListViewItem item in removes)
                    (item.Tag as TaskCanselToken).cts.Cancel();
            }
            return (listView_TaskList.Items.Count > 0);
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
        string root_id;
        bool supressListviewRefresh = false;
        private int CriticalCount = 0;

        Dictionary<string, ItemInfo> DriveTree = new Dictionary<string, ItemInfo>();
        FileMetadata_Info[] treedata = null;

        private async Task Login()
        {
            Config.Log.LogOut("Login Start.");
            var task = CreateTask("login");
            var ct = task.cts.Token;
            try
            {
                // Login & GetEndpoint
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Login ...";
                if (await Drive.Login(ct) &&
                    await Drive.GetEndpoint(ct))
                {
                    initialized = true;
                    loginToolStripMenuItem.Enabled = false;
                    toolStripMenuItem_Logout.Enabled = true;
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
            finally
            {
                FinishTask(task);
            }
        }

        private async Task Logout()
        {
            Config.Log.LogOut("Logout Start.");
            if (CancelTaskAll())
            {
                while (listView_TaskList.Items.Count > 0)
                    await Task.Delay(100);
            }
            initialized = false;
            Drive = new AmazonDrive();
            Config.refresh_token = "";
            Config.Save();
            loginToolStripMenuItem.Enabled = true;
            toolStripMenuItem_Logout.Enabled = false;
        }

        public static bool SaveToBinaryFile(object obj, string path)
        {
            try
            {
                using (var fs = new FileStream(path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (var ds = new GZipStream(fs, CompressionLevel.Optimal))
                {
                    var bf = new BinaryFormatter();
                    //シリアル化して書き込む
                    bf.Serialize(ds, obj);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object LoadFromBinaryFile(string path)
        {
            using (var fs = new FileStream(path,
                FileMode.Open,
                FileAccess.Read))
            using (var ds = new GZipStream(fs, CompressionMode.Decompress))
            {
                BinaryFormatter f = new BinaryFormatter();
                //読み込んで逆シリアル化する
                return f.Deserialize(ds);
            }
        }

        const string cachefile = "drivecache.bin";

        private async Task InitAlltree()
        {
            var task = CreateTask("Init drive tree data");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading treedata...";
                while (true)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            treedata = (FileMetadata_Info[])LoadFromBinaryFile(cachefile);
                        }, ct);
                        break;
                    }
                    catch
                    {
                        // Load Root
                        toolStripStatusLabel1.Text = "Loading Root...";
                        var rootdata = await Drive.ListMetadata("", ct: ct);
                        toolStripStatusLabel1.Text = "RootNode Loaded.";
                        treedata = rootdata.data;
                        if (SaveToBinaryFile(treedata, cachefile))
                            break;
                    }
                }
                toolStripStatusLabel1.Text = "Create tree...";
                ConstructDriveTree(treedata);
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "done.";
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
            finally
            {
                FinishTask(task);
            }
        }

        private void ConstructDriveTree(FileMetadata_Info[] newdata)
        {
            foreach (var item in newdata)
            {
                ItemInfo value;
                if (DriveTree.TryGetValue(item.id, out value))
                {
                    value.info = item;
                }
                else
                {
                    DriveTree[item.id] = new ItemInfo(item);
                }
                foreach (var p in item.parents)
                {
                    if (DriveTree.TryGetValue(p, out value))
                    {
                        value.children[item.id] = DriveTree[item.id];
                    }
                    else
                    {
                        DriveTree[p] = new ItemInfo(null);
                        DriveTree[p].children[item.id] = DriveTree[item.id];
                    }
                }
                if (item.isRoot ?? false)
                    root_id = item.id;
            }
        }

        private void AddNewDriveItem(FileMetadata_Info newdata)
        {
            ItemInfo value;
            if (DriveTree.TryGetValue(newdata.id, out value))
            {
                value.info = newdata;
            }
            else
            {
                DriveTree[newdata.id] = new ItemInfo(newdata);
            }
            foreach (var p in newdata.parents)
            {
                if (DriveTree.TryGetValue(p, out value))
                {
                    value.children[newdata.id] = DriveTree[newdata.id];
                }
                else
                {
                    DriveTree[p] = new ItemInfo(null);
                    DriveTree[p].children[newdata.id] = DriveTree[newdata.id];
                }
            }
        }

        private TreeNode[] GenerateTreeNode(IEnumerable<ItemInfo> children, int count = 0)
        {
            return children.Select(x =>
            {
                int img = (x.info.kind == "FOLDER") ? 0 : 2;
                var node = new TreeNode(x.info.name, img, img);
                node.Name = x.info.name;
                node.Tag = x;
                if (x.info.kind == "FOLDER" && count > 0)
                {
                    node.Nodes.AddRange(GenerateTreeNode(x.children.Values, count - 1));
                }
                ItemInfo value;
                if (DriveTree.TryGetValue(x.info.id, out value))
                {
                    value.tree = node;
                }
                else
                {
                    DriveTree[x.info.id] = new ItemInfo(null);
                    DriveTree[x.info.id].tree = node;
                }
                return node;
            }).ToArray();
        }

        private string GetFullPathfromItem(ItemInfo info)
        {
            if (info.info.id == root_id) return "/";
            else
            {
                var parents = GetFullPathfromItem(DriveTree[info.info.parents[0]]);
                return parents + ((parents != "/") ? "/" : "") + info.info.name;
            }
        }

        private ListViewItem[] GenerateListViewItem(ItemInfo root)
        {
            List<ListViewItem> ret = new List<ListViewItem>();

            var path = GetFullPathfromItem(root);
            var rootitem = new ListViewItem(
                new string[] {
                            ".",
                            "",
                            root.info.modifiedDate.ToString(),
                            root.info.createdDate.ToString(),
                            path,
                            root.info.id,
                            "",
                }, 0);
            rootitem.Tag = root;
            rootitem.Name = (root.info.id == root_id) ? "/" : ".";
            rootitem.ToolTipText = "このフォルダ自身";
            ret.Add(rootitem);

            var up = (root.info.id == root_id) ? DriveTree[root.info.id] : DriveTree[root.info.parents[0]];
            path = GetFullPathfromItem(up);
            var upitem = new ListViewItem(
                new string[] {
                            "..",
                            "",
                            up.info.modifiedDate.ToString(),
                            up.info.createdDate.ToString(),
                            path,
                            up.info.id,
                            "",
                }, 0);
            upitem.Tag = up;
            upitem.Name = (up.info.id == root_id) ? "/" : "..";
            upitem.ToolTipText = "ひとつ上のフォルダ";
            ret.Add(upitem);

            var childitem = root.children.Values.Select(x =>
            {
                path = GetFullPathfromItem(x);
                var item = new ListViewItem(
                    new string[] {
                            x.info.name,
                            x.info.contentProperties?.size?.ToString("#,0"),
                            x.info.modifiedDate.ToString(),
                            x.info.createdDate.ToString(),
                            path,
                            x.info.id,
                            x.info.contentProperties?.md5,
                    }, (x.info.kind == "FOLDER") ? 0 : 2);
                item.Name = x.info.name;
                item.Tag = x;
                item.ToolTipText = item.Name;
                return item;
            });
            ret.AddRange(childitem);

            return ret.ToArray();
        }

        private ListViewItem[] GenerateListViewItem(ItemInfo[] Items)
        {
            List<ListViewItem> ret = new List<ListViewItem>();

            var childitem = Items.Select(x =>
            {
                var path = GetFullPathfromItem(x);
                var item = new ListViewItem(
                    new string[] {
                            x.info.name,
                            x.info.contentProperties?.size?.ToString("#,0"),
                            x.info.modifiedDate.ToString(),
                            x.info.createdDate.ToString(),
                            path,
                            x.info.id,
                            x.info.contentProperties?.md5,
                    }, (x.info.kind == "FOLDER") ? 0 : 2);
                item.Name = x.info.name;
                item.Tag = x;
                item.ToolTipText = item.Name;
                return item;
            });
            ret.AddRange(childitem);

            return ret.ToArray();
        }

        private async Task InitView()
        {
            // Load Drive Tree
            await InitAlltree();

            // Refresh Drive Tree
            await ReloadItems(root_id);
        }

        private void InitializeListView()
        {
            // ListViewコントロールのプロパティを設定
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.Sorting = SortOrder.Ascending;

            // 列（コラム）ヘッダの作成
            listView1.Columns.Add("Name", 200);
            listView1.Columns.Add("Size", 90);
            listView1.Columns.Add("modifiedDate", 120);
            listView1.Columns.Add("createdDate", 120);
            listView1.Columns.Add("path", 100);
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
            lvwColumnSorter.Order = SortOrder.Ascending;
            listView1.ListViewItemSorter = lvwColumnSorter;
            listView1.Sort();
        }

        private void LoadTreeItem(TreeNode node)
        {
            var nodedata = node.Tag as ItemInfo;
            if (nodedata.info.kind != "FOLDER") return;

            foreach (TreeNode child in node.Nodes)
            {
                child.Nodes.Clear();
                child.Nodes.AddRange(GenerateTreeNode((child.Tag as ItemInfo).children.Values));
            }
        }

        private string PathtoID(string path_str)
        {
            string id = root_id;
            if (string.IsNullOrEmpty(path_str)) return id;
            var path = path_str.Split('/', '\\');
            foreach (var p in path)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var find_result = DriveTree[id].children.Where(x => x.Value.info.name == p);
                if (find_result.Count() == 0) break;
                id = find_result.First().Key;
            }
            return id;
        }

        private void FollowPath(string path_str)
        {
            string target_id = PathtoID(path_str);

            if (target_id != root_id)
            {
                if (DriveTree[target_id].tree == null)
                {
                    // not loaded tree
                    List<string> tree_ids = new List<string>();
                    tree_ids.Add(target_id);
                    var p = DriveTree[target_id].info.parents[0];
                    while (DriveTree[p].tree == null)
                    {
                        tree_ids.Add(p);
                        p = DriveTree[p].info.parents[0];
                    }
                    tree_ids.Reverse();
                    DriveTree[p].tree.Nodes.AddRange(GenerateTreeNode(DriveTree[p].children.Values));
                    foreach (var t in tree_ids)
                    {
                        DriveTree[t].tree.Nodes.AddRange(GenerateTreeNode(DriveTree[t].children.Values));
                    }
                }
                treeView1.SelectedNode = DriveTree[target_id].tree;
                treeView1.SelectedNode.Expand();
            }

            //// display listview Root
            listView1.Items.Clear();
            listView1.Items.AddRange(GenerateListViewItem(DriveTree[target_id]));
            listView1.Sort();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            LoadImage();
            listView_TaskList.Columns[0].Width = listView_TaskList.Width - 25;
            logToFileToolStripMenuItem.Checked = Config.LogToFile;
            textBox_HostName.Text = Config.SendToHost;
            textBox_Port.Text = Config.SendToPort.ToString();
            textBox_SendPacketNum.Text = Config.SendPacketNum.ToString();
            textBox_SendDelay.Text = Config.SendDelay.ToString();
            textBox_SendLongOffset.Text = Config.SendLongOffset.ToString();
            textBox_SendRatebySendCount.Text = Config.SendRatebySendCount.ToString();
            textBox_SendRatebyTOTCount.Text = Config.SendRatebyTOTCount.ToString();
            textBox_VK.Text = Config.SendVK.ToString();
            textBox_keySendApp.Text = Config.SendVK_Application;
            SetBandwidthInfo();
            await Login();
        }

        private int SelectBase(double value)
        {
            double value10 = value / 1000;
            double value2 = value / 1024;
            if (value10 < 1000 || value2 < 1024)
            {
                // 小数点以下がない方の基数を選択する
                if (value10 % 1.0 == 0)
                {
                    return 10;
                }
                if (value2 % 1.0 == 0)
                {
                    return 2;
                }

                // 小数点1位以下がない方の基数を選択する
                if (value10 % 0.1 == 0)
                {
                    return 10;
                }
                if (value2 % 0.1 == 0)
                {
                    return 2;
                }

                // 小数点2位以下がない方の基数を選択する
                if (value10 % 0.01 == 0)
                {
                    return 10;
                }
                if (value2 % 0.01 == 0)
                {
                    return 2;
                }

                // デフォルトの基数は2
                return 2;
            }
            else
            {
                if (SelectBase(value10) == 10) return 10;
                else return 2;
            }
        }

        private void SetBandwidthInfo()
        {
            if (double.IsPositiveInfinity(Config.UploadLimit) || Config.UploadLimit <= 0)
            {
                Config.UploadLimit = double.PositiveInfinity;
                textBox_UploadBandwidthLimit.Text = "";
                comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("Infinity");
            }
            else
            {
                double value = Config.UploadLimit;
                if (value < 1000)
                {
                    textBox_UploadBandwidthLimit.Text = value.ToString();
                    comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("Byte/s");
                }
                else
                {
                    if (SelectBase(value) == 10)
                    {
                        if (value > 1000 * 1000 * 1000)
                        {
                            textBox_UploadBandwidthLimit.Text = (value / (1000 * 1000 * 1000)).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("GB/s");
                        }
                        else if (value > 1000 * 1000)
                        {
                            textBox_UploadBandwidthLimit.Text = (value / (1000 * 1000)).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("MB/s");
                        }
                        else
                        {
                            textBox_UploadBandwidthLimit.Text = (value / 1000).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("KB/s");
                        }
                    }
                    else
                    {
                        if (value > 1024 * 1024 * 1024)
                        {
                            textBox_UploadBandwidthLimit.Text = (value / (1024 * 1024 * 1024)).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("GiB/s");
                        }
                        else if (value > 1024 * 1024)
                        {
                            textBox_UploadBandwidthLimit.Text = (value / (1024 * 1024)).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("MiB/s");
                        }
                        else
                        {
                            textBox_UploadBandwidthLimit.Text = (value / 1024).ToString();
                            comboBox_UploadLimitUnit.SelectedIndex = comboBox_UploadLimitUnit.Items.IndexOf("KiB/s");
                        }
                    }
                }
            }

            if (double.IsPositiveInfinity(Config.DownloadLimit) || Config.DownloadLimit <= 0)
            {
                Config.DownloadLimit = double.PositiveInfinity;
                textBox_DownloadBandwidthLimit.Text = "";
                comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("Infinity");
            }
            else
            {
                double value = Config.DownloadLimit;
                if (value < 1000)
                {
                    textBox_DownloadBandwidthLimit.Text = value.ToString();
                    comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("Byte/s");
                }
                else
                {
                    if (SelectBase(value) == 10)
                    {
                        if (value > 1000 * 1000 * 1000)
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / (1000 * 1000 * 1000)).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("GB/s");
                        }
                        else if (value > 1000 * 1000)
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / (1000 * 1000)).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("MB/s");
                        }
                        else
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / 1000).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("KB/s");
                        }
                    }
                    else
                    {
                        if (value > 1024 * 1024 * 1024)
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / (1024 * 1024 * 1024)).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("GiB/s");
                        }
                        else if (value > 1024 * 1024)
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / (1024 * 1024)).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("MiB/s");
                        }
                        else
                        {
                            textBox_DownloadBandwidthLimit.Text = (value / 1024).ToString();
                            comboBox_DownloadLimitUnit.SelectedIndex = comboBox_DownloadLimitUnit.Items.IndexOf("KiB/s");
                        }
                    }
                }
            }
        }

        static int WM_CLOSE = 0x10;

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CancelTaskAll() || CriticalCount > 0)
            {
                e.Cancel = true;
                PostMessage(Handle, WM_CLOSE, 0, 0);
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

        private async void toolStripMenuItem_Logout_Click(object sender, EventArgs e)
        {
            await Logout();
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

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            LoadTreeItem(e.Node);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            textBox_path.Text = e.Node.FullPath;

            if (!supressListviewRefresh)
            {
                if (e.Node == null)
                {
                    listView1.Items.Clear();
                    listView1.Items.AddRange(GenerateListViewItem(DriveTree[root_id]));
                    listView1.Sort();
                    return;
                }

                var selectdata = e.Node.Tag as ItemInfo;
                if (selectdata == null) return;

                if (selectdata.info.kind == "FOLDER")
                {
                    listView1.Items.Clear();
                    listView1.Items.AddRange(GenerateListViewItem(selectdata));
                    listView1.Sort();
                }
                else
                {
                    listView1.Items.Clear();
                    listView1.Items.AddRange(GenerateListViewItem(DriveTree[selectdata.info.parents[0]]));
                    listView1.Sort();
                }
            }
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

        private async void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var selectitem = listView1.SelectedItems[0];

            selectitem.Selected = false;
            selectitem.Focused = false;

            var selectdata = selectitem.Tag as ItemInfo;
            if (selectdata == null) return;
            if (selectdata.info.kind == "FOLDER")
            {
                listView1.Items.Clear();
                listView1.Items.AddRange(GenerateListViewItem(selectdata));
                listView1.Sort();
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_SendUDP")
            {
                selectitem.Selected = true;
                await CancelTask("play");
                await PlayFiles(PlayOneTSFile, "Send UDP");
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_FFmpeg")
            {
                selectitem.Selected = true;
                await PlayWithFFmpeg();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var selectitem = listView1.SelectedItems[0];

            var tree = (selectitem.Tag as ItemInfo)?.tree;
            if (tree == null) return;

            supressListviewRefresh = true;
            try
            {
                treeView1.SelectedNode = tree;
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

        private async Task<int> DoFileUpload(IEnumerable<string> Filenames, string parent_id, int f_all, int f_cur, CancellationToken ct = default(CancellationToken))
        {
            FileMetadata_Info[] done_files = null;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = CreateTask("FileUpload");
                ct = task.cts.Token;
            }
            try
            {
                if (checkBox_upSkip.Checked)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    toolStripStatusLabel1.Text = "Check Drive files...";
                    var ret = await Drive.ListChildren(parent_id, ct: ct);
                    done_files = ret.data;

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripStatusLabel1.Text = "Check done.";
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 100;
                }

                foreach (var filename in Filenames)
                {
                    ct.ThrowIfCancellationRequested();
                    Config.Log.LogOut("Upload File: " + filename);
                    var upload_str = (f_all > 1) ? string.Format("Upload({0}/{1})...", ++f_cur, f_all) : "Upload...";
                    var short_filename = System.IO.Path.GetFileName(filename);

                    if (done_files?.Select(x => x.name).Contains(short_filename) ?? false)
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
                                await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct);
                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                toolStripStatusLabel1.Text = "Check done.";
                                if (BitConverter.ToString(md5).ToLower().Replace("-", "") == target.contentProperties?.md5)
                                    continue;
                            }
                        }
                    }

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 10000;
                    toolStripStatusLabel1.Text = upload_str + " " + short_filename;

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
                                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                            toolStripProgressBar1.Maximum = 10000;
                                            toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                        }, evnt);
                                }, ct: ct);
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

                        Config.Log.LogOut("Upload faild." + retry.ToString());
                        // wait for retry
                        while (--checkretry > 0)
                        {
                            try
                            {
                                Config.Log.LogOut("Upload : wait 10sec for retry..." + checkretry.ToString());
                                await Task.Delay(TimeSpan.FromSeconds(10), ct);

                                var children = await Drive.ListChildren(parent_id, ct: ct);
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
            finally
            {
                FinishTask(task);
            }
        }

        private async Task<int> DoDirectoryUpload(IEnumerable<string> Filenames, string parent_id, int f_all, int f_cur, CancellationToken ct = default(CancellationToken))
        {
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = CreateTask("DirectoryUpload");
                ct = task.cts.Token;
            }
            try
            {
                foreach (var filename in Filenames)
                {
                    ct.ThrowIfCancellationRequested();
                    var short_name = Path.GetFullPath(filename).Split(new char[] { '\\', '/' }).Last();

                    // make subdirectory
                    var newdir = await Drive.createFolder(short_name, parent_id);

                    f_cur = await DoFileUpload(Directory.EnumerateFiles(filename), newdir.id, f_all, f_cur, ct);
                    if (f_cur < 0) return -1;

                    f_cur = await DoDirectoryUpload(Directory.EnumerateDirectories(filename), newdir.id, f_all, f_cur, ct);
                    if (f_cur < 0) return -1;
                }
                return f_cur;
            }
            finally
            {
                FinishTask(task);
            }
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

            try
            {
                int f_all = openFileDialog1.FileNames.Count();
                int f_cur = 0;
                string parent_id = null;
                ItemInfo target = null;
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    if (currect.Length == 0) return;
                    target = (currect[0].Tag as ItemInfo);
                    parent_id = target.info.id;
                }
                catch { }

                if (await DoFileUpload(openFileDialog1.FileNames, parent_id, f_all, f_cur) < 0) return;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReloadItems(parent_id);
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

            var task = CreateTask("TrashItem");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items...";
                toolStripProgressBar1.Maximum = select.Count;
                toolStripProgressBar1.Step = 1;

                ItemInfo target = null;
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    target = (currect[0].Tag as ItemInfo);
                }
                catch { }

                foreach (ListViewItem item in select)
                {
                    var ret = await Drive.TrashItem((item.Tag as ItemInfo).info.id, ct: ct);
                    toolStripProgressBar1.PerformStep();
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReloadItems(target?.info.id);
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
            finally
            {
                FinishTask(task);
            }
        }

        private void RemoveDriveTreeChild(string id)
        {
            var item = DriveTree[id];
            DriveTree.Remove(id);
            foreach (var child in item.children.Values)
            {
                RemoveDriveTreeChild(child.info.id);
            }
        }

        private async Task ForceReloadItems(string display_id)
        {
            if (string.IsNullOrEmpty(display_id))
                display_id = root_id;
            await CancelTask("ReloadItems");
            await CancelTask("ForceReloadItems");
            var task = CreateTask("ForceReloadItems");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading...";

                // Load Root
                toolStripStatusLabel1.Text = "Loading Root...";
                FileListdata_Info rootdata;
                while (true)
                {
                    rootdata = await Drive.ListMetadata("", ct: ct);
                    toolStripStatusLabel1.Text = "RootNode Loaded.";
                    treedata = rootdata.data;
                    if (SaveToBinaryFile(treedata, cachefile))
                        break;
                }
                // load tree
                var items = GenerateTreeNode(DriveTree[root_id].children.Values, 1);
                treeView1.Nodes.Clear();
                treeView1.Nodes.AddRange(items);

                List<string> tree_ids = new List<string>();
                tree_ids.Add(display_id);
                var p = display_id;
                while (p != root_id)
                {
                    p = DriveTree[p].info.parents[0];
                    tree_ids.Add(p);
                }
                tree_ids.Reverse();
                var Nodes = treeView1.Nodes;
                foreach (var t in tree_ids)
                {
                    if (t == root_id) continue;
                    var i = Nodes.OfType<TreeNode>().Where(x => (x.Tag as ItemInfo).info.id == t);
                    if (i.Count() > 0)
                    {
                        treeView1.SelectedNode = i.First();
                        LoadTreeItem(treeView1.SelectedNode);
                        Nodes = treeView1.SelectedNode.Nodes;
                    }
                    else break;
                }
                treeView1.SelectedNode?.Expand();

                //// display listview Root
                listView1.Items.Clear();
                listView1.Items.AddRange(GenerateListViewItem(DriveTree[display_id]));
                listView1.Sort();

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "done.";
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
            finally
            {
                FinishTask(task);
            }
        }

        private async Task ReloadItems(string display_id)
        {
            if (string.IsNullOrEmpty(display_id))
                display_id = root_id;

            await CancelTask("ReloadItems");
            await CancelTask("ForceReloadItems");
            var task = CreateTask("ReloadItems");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading...";

                DateTime lastload = default(DateTime);
                try
                {
                    lastload = File.GetLastWriteTime(cachefile) - TimeSpan.FromSeconds(60);
                }
                catch { }

                // Load Changed items
                var datestr = lastload.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                var rootdata = await Drive.ListMetadata("modifiedDate:[" + datestr + " TO *}", ct: ct);
                ConstructDriveTree(rootdata.data);
                foreach (var folder in rootdata.data?.Where(x => x.kind == "FOLDER"))
                {
                    var children = await Drive.ListChildren(folder.id);
                    foreach (var oldChild in DriveTree[folder.id].children)
                    {
                        if (children.data.Where(x => x.id == oldChild.Value.info.id).Count() == 0)
                        {
                            if (oldChild.Value.info.parents.Count() == 1 &&
                                oldChild.Value.info.parents[0] == folder.id)
                            {
                                DriveTree.Remove(oldChild.Value.info.id);
                            }
                        }
                    }
                    DriveTree[folder.id].children.Clear();
                    ConstructDriveTree(children.data);
                }
                var removekey = DriveTree.Where(x =>
                {
                    if (x.Value.info == null) return true;
                    if (x.Value.info.id == root_id) return false;
                    foreach (var parent in x.Value.info.parents)
                    {
                        if (DriveTree.ContainsKey(parent))
                        {
                            return false;
                        }
                    }
                    return true;
                }).ToArray();
                foreach (var key in removekey)
                {
                    RemoveDriveTreeChild(key.Key);
                }
                treedata = DriveTree.Values.Select(x => x.info).Where(x => x != null).ToArray();
                if (!SaveToBinaryFile(treedata, cachefile))
                {
                    await ForceReloadItems(display_id);
                    return;
                }

                // load tree
                var items = GenerateTreeNode(DriveTree[root_id].children.Values, 1);
                treeView1.Nodes.Clear();
                treeView1.Nodes.AddRange(items);

                List<string> tree_ids = new List<string>();
                tree_ids.Add(display_id);
                var p = display_id;
                while (p != root_id)
                {
                    p = DriveTree[p].info.parents[0];
                    tree_ids.Add(p);
                }
                tree_ids.Reverse();
                var Nodes = treeView1.Nodes;
                foreach (var t in tree_ids)
                {
                    if (t == root_id) continue;
                    var i = Nodes.OfType<TreeNode>().Where(x => (x.Tag as ItemInfo).info.id == t);
                    if (i.Count() > 0)
                    {
                        treeView1.SelectedNode = i.First();
                        LoadTreeItem(treeView1.SelectedNode);
                        Nodes = treeView1.SelectedNode.Nodes;
                    }
                    else break;
                }
                treeView1.SelectedNode?.Expand();

                //// display listview Root
                listView1.Items.Clear();
                listView1.Items.AddRange(GenerateListViewItem(DriveTree[display_id]));
                listView1.Sort();

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "done.";
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
            finally
            {
                FinishTask(task);
            }
        }

        private async void button_reload_Click(object sender, EventArgs e)
        {
            var currect = listView1.Items.Find(".", false);
            string target_id = root_id;
            if (currect.Length > 0) target_id = (currect[0].Tag as ItemInfo).info.id;

            if ((ModifierKeys & Keys.Shift) == Keys.Shift ||
                (ModifierKeys & Keys.Control) == Keys.Control)
            {
                await ForceReloadItems(target_id);
            }
            else
            {
                await ReloadItems(target_id);
            }
        }

        private async void downloadItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Download Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as ItemInfo).info).Where(x => x.kind != "FOLDER").ToArray();

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            int f_cur = 0;
            string savefilename = null;
            string savefilepath = null;
            string error_log = "";
            var task = CreateTask("downloads");
            var ct = task.cts.Token;
            try
            {
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
                    ct.ThrowIfCancellationRequested();
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
                            using (var outfile = File.Open(savefilename, FileMode.Create, FileAccess.Write, FileShare.Read))
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
                                            var ret = await Drive.downloadFile(downitem.id, ct: ct);
                                            var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                            f.PosChangeEvent += (src, evnt) =>
                                            {
                                                synchronizationContext.Post(
                                                    (o) =>
                                                    {
                                                        if (ct.IsCancellationRequested) return;
                                                        var eo = o as PositionChangeEventArgs;
                                                        toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                                        toolStripProgressBar1.Maximum = 10000;
                                                        toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                                    }, evnt);
                                            };
                                            await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct);
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
                                    using (var ret = await Drive.downloadFile(downitem.id, ct: ct))
                                    using (var f = new PositionStream(ret, downitem.contentProperties.size.Value))
                                    {
                                        f.PosChangeEvent += (src, evnt) =>
                                        {
                                            synchronizationContext.Post(
                                                (o) =>
                                                {
                                                    if (ct.IsCancellationRequested) return;
                                                    var eo = o as PositionChangeEventArgs;
                                                    toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                                    toolStripProgressBar1.Maximum = 10000;
                                                    toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                                }, evnt);
                                        };
                                        await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct);
                                    }
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
            finally
            {
                FinishTask(task);
            }
            if (error_log != "")
            {
                MessageBox.Show("Download : WARNING\r\n" + error_log);
            }
        }

        private async void sendUDPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await CancelTask("play");
            await PlayFiles(PlayOneTSFile, "Send UDP");
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            comboBox_FindStr.Items.Add(comboBox_FindStr.Text);

            ItemInfo[] selection = DriveTree.Values.ToArray();

            if (!checkBox_File.Checked && !checkBox_Folder.Checked)
            {
                //nothing
            }
            else
            {
                if (!checkBox_File.Checked)
                    selection = selection.Where(x => x.info.kind == "FOLDER").ToArray();
                if (!checkBox_Folder.Checked)
                    selection = selection.Where(x => x.info.kind != "FOLDER").ToArray();
            }

            if (checkBox_Regex.Checked)
                selection = selection.Where(x => Regex.IsMatch(x.info.name, comboBox_FindStr.Text)).ToArray();
            else
                selection = selection.Where(x => (x.info.name?.IndexOf(comboBox_FindStr.Text) >= 0)).ToArray();

            if (radioButton_createTime.Checked)
            {
                if (checkBox_dateFrom.Checked)
                    selection = selection.Where(x => x.info.createdDate > dateTimePicker_from.Value).ToArray();
                if (checkBox_dateTo.Checked)
                    selection = selection.Where(x => x.info.createdDate < dateTimePicker_to.Value).ToArray();
            }
            if (radioButton_modifiedDate.Checked)
            {
                if (checkBox_dateFrom.Checked)
                    selection = selection.Where(x => x.info.modifiedDate > dateTimePicker_from.Value).ToArray();
                if (checkBox_dateTo.Checked)
                    selection = selection.Where(x => x.info.modifiedDate < dateTimePicker_to.Value).ToArray();
            }

            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
            toolStripStatusLabel1.Text = "Found : " + selection.Length.ToString();

            listView1.Items.Clear();
            listView1.Items.AddRange(GenerateListViewItem(selection));
        }

        private async void button_mkdir_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("make Folder Start.");
            toolStripStatusLabel1.Text = "unable to mkFolder.";
            if (!initialized) return;
            toolStripStatusLabel1.Text = "mkFolder...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            try
            {
                string parent_id = null;
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    parent_id = (currect[0].Tag as ItemInfo).info.id;
                }
                catch { }

                var newdir = await Drive.createFolder(textBox_newName.Text, parent_id);

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReloadItems(parent_id);
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

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as ItemInfo).info).ToArray();

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            if (f_all > 1)
                if (MessageBox.Show("Do you want to rename multiple items?", "Rename Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            toolStripStatusLabel1.Text = "Rename...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            var task = CreateTask("Rename");
            var ct = task.cts.Token;
            try
            {
                string parent_id = null;
                try
                {
                    var currect = listView1.Items.Find(".", false);
                    parent_id = (currect[0].Tag as ItemInfo).info.id;
                }
                catch { }

                foreach (var downitem in selectItem)
                {
                    using (var NewName = new FormInputName())
                    {
                        NewName.NewItemName = downitem.name;
                        if (NewName.ShowDialog() != DialogResult.OK) break;

                        ct.ThrowIfCancellationRequested();
                        var tmpfile = await Drive.renameItem(downitem.id, NewName.NewItemName, ct: ct);
                    }
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Rename Items done.";

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReloadItems(parent_id);
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
            finally
            {
                FinishTask(task);
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

                if (listView1.Items.IndexOf(item) < 0 || (item.Name != "/" && (item?.Tag as ItemInfo).info.kind != "FOLDER"))
                {
                    if (current.Length > 0) item = current[0];
                }

                if (item != null)
                {
                    if (item.Tag == null || item.Name == "/" || (item.Tag as ItemInfo).info.kind == "FOLDER")
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                            e.Effect = DragDropEffects.Copy;
                        else
                        {
                            if (item != ((current.Length > 0) ? current[0] : null) &&
                                ((item.Name == "/" && ((current.Length > 0) ? current[0] : null).Name != "/") ||
                                !(((ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))?.Contains(item) ?? false)))
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
                    if (listView1.Items.IndexOf(item) < 0 || (item.Name != "/" && (item?.Tag as ItemInfo).info.kind != "FOLDER"))
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

                        string parent_id = null;
                        try
                        {
                            var currect = listView1.Items.Find(".", false);
                            parent_id = (currect[0].Tag as ItemInfo).info.id;
                        }
                        catch { }

                        try
                        {
                            var selects = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));
                            int count = 0;
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item...";
                            toolStripProgressBar1.Maximum = selects.Count;
                            toolStripProgressBar1.Step = 1;

                            var toParent = (item.Tag == null) ? root_id : (item.Tag as ItemInfo).info.id;
                            foreach (ListViewItem aItem in selects)
                            {
                                var fromParent = (aItem.Tag as ItemInfo).info.parents[0];
                                var childid = (aItem.Tag as ItemInfo).info.id;
                                toolStripStatusLabel1.Text = string.Format("Move Item... {0}/{1} {2}", ++count, selects.Count, (aItem.Tag as ItemInfo).info.name);

                                await Drive.moveChild(childid, fromParent, toParent);
                                toolStripProgressBar1.PerformStep();
                            }

                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item done.";
                            toolStripProgressBar1.Maximum = 100;
                            toolStripProgressBar1.Step = 10;

                            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            await ReloadItems(parent_id);
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

                        var task = CreateTask("upload(listview)");
                        try
                        {
                            int f_all = drags.Length + dir_drags.Select(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).SelectMany(i => i).Distinct().Count();
                            int f_cur = 0;
                            string parent_id = null;
                            try
                            {
                                parent_id = (item.Tag as ItemInfo).info.id;
                            }
                            catch { }

                            try
                            {
                                f_cur = await DoFileUpload(drags, parent_id, f_all, f_cur);
                                if (f_cur >= 0)
                                    f_cur = await DoDirectoryUpload(dir_drags, parent_id, f_all, f_cur, task.cts.Token);

                                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                await ReloadItems(parent_id);
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
                        finally
                        {
                            FinishTask(task);
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

            if (HoldonNode != item)
            {
                HoldonNode = null;
                return;
            }

            supressListviewRefresh = true;
            try
            {
                var children_kind = item.Nodes.OfType<TreeNode>().Select(x => (x.Tag as ItemInfo).info.kind);
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

                if (item == null || !string.IsNullOrEmpty((item.Tag as ItemInfo).info.kind))
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
                            while ((item.Tag as ItemInfo).info.kind != "FOLDER")
                            {
                                item = item.Parent;
                                if (item == null) break;
                            }
                        }
                        var toParent = (item == null) ? root_id : (item.Tag as ItemInfo).info.id;
                        foreach (ListViewItem aItem in (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection)))
                        {
                            var fromParent = (aItem.Tag as ItemInfo).info.parents[0];
                            var childid = (aItem.Tag as ItemInfo).info.id;
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
                        while ((item.Tag as ItemInfo).info.kind != "FOLDER")
                        {
                            item = item.Parent;
                            if (item == null) break;
                        }
                    }

                    string disp_id = null;
                    try
                    {
                        var currect = listView1.Items.Find(".", false);
                        disp_id = (currect[0].Tag as ItemInfo).info.id;
                    }
                    catch { }

                    if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
                    {
                        Config.Log.LogOut("move(treeview) Start.");
                        toolStripStatusLabel1.Text = "Move Item...";
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

                        try
                        {
                            var selects = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));
                            int count = 0;
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item...";
                            toolStripProgressBar1.Maximum = selects.Count;
                            toolStripProgressBar1.Step = 1;


                            var toParent = (item == null) ? root_id : (item.Tag as ItemInfo).info.id;
                            foreach (ListViewItem aItem in selects)
                            {
                                var fromParent = (aItem.Tag as ItemInfo).info.parents[0];
                                var childid = (aItem.Tag as ItemInfo).info.id;
                                toolStripStatusLabel1.Text = string.Format("Move Item... {0}/{1} {2}", ++count, selects.Count, (aItem.Tag as ItemInfo).info.name);

                                await Drive.moveChild(childid, fromParent, toParent);
                                toolStripProgressBar1.PerformStep();
                            }
                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                            toolStripStatusLabel1.Text = "Move Item done.";
                            toolStripProgressBar1.Maximum = 100;
                            toolStripProgressBar1.Step = 10;

                            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            await ReloadItems(disp_id);
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
                        var parent_id = (item.Tag as ItemInfo).info.id;

                        var task = CreateTask("upload(treeview)");
                        try
                        {
                            f_cur = await DoFileUpload(drags, parent_id, f_all, f_cur);
                            if (f_cur >= 0)
                                f_cur = await DoDirectoryUpload(dir_drags, parent_id, f_all, f_cur, task.cts.Token);
                        }
                        finally
                        {
                            FinishTask(task);
                        }

                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await ReloadItems(disp_id);
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
            catch
            {
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
            if (e.KeyData == (Keys.A | Keys.Control))
            {
                listView1.BeginUpdate();
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Name != "." && item.Name != ".." && item.Name != "/")
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
                (listView1.SelectedItems.Count == 0 ? listView1.Items.OfType<ListViewItem>() : listView1.SelectedItems.OfType<ListViewItem>())
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

        ////////////////////////////////////////////////////////////////////////
        /// 
        /// send a file with UDP
        /// 
        ////////////////////////////////////////////////////////////////////////

        private async void button_Play_Click(object sender, EventArgs e)
        {
            await CancelTask("play");
            await PlayFiles(PlayOneTSFile, "Send UDP");
        }

        private TimeSpan SendDuration;
        private TimeSpan SendStartDelay;
        private DateTime SendStartTime;

        private TimeSpan SeekUDPtoPos = TimeSpan.FromDays(100);
        CancellationTokenSource seekUDP_ct_source = new CancellationTokenSource();

        private int nextUDPcount = 0;

        private void CancelForSeekUDP()
        {
            var t = seekUDP_ct_source;
            seekUDP_ct_source = new CancellationTokenSource();
            t.Cancel();
        }

        private async Task PlayOneTSFile(FileMetadata_Info downitem, string download_str, CancellationToken ct, object data)
        {
            long bytePerSec = 0;
            long? SkipByte = null;
            DateTime InitialTOT = default(DateTime);

            trackBar_Pos.Tag = 1;
            trackBar_Pos.Minimum = 0;
            trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / (10 / 8 * 1024 * 1024));
            trackBar_Pos.Value = 0;
            trackBar_Pos.Tag = 0;

            while (true)
            {
                PressKeyForOtherApp();

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripProgressBar1.Maximum = 10000;
                toolStripStatusLabel1.Text = download_str + " " + downitem.name;

                var internalToken = seekUDP_ct_source.Token;
                var externalToken = ct;
                try
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(internalToken, externalToken))
                    using (var ret = await Drive.downloadFile(downitem.id, SkipByte, ct: linkedCts.Token))
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
                                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                    toolStripProgressBar1.Maximum = 10000;
                                    toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                }, evnt);
                        };
                        using (var UDP = new UDP_TS_Stream(linkedCts.Token))
                        {
                            label_sendname.Text = downitem.name;
                            if (SeekUDPtoPos < TimeSpan.FromDays(30))
                            {
                                if (SendDuration != default(TimeSpan))
                                    UDP.SendDuration = SendDuration - SeekUDPtoPos;

                                if (InitialTOT != default(DateTime))
                                    UDP.SendStartTime = InitialTOT + SeekUDPtoPos;
                                else
                                    UDP.SendDelay = SeekUDPtoPos;
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
                                        if (linkedCts.Token.IsCancellationRequested) return;
                                        var eo = o as TOTChangeEventArgs;
                                        if (InitialTOT == default(DateTime))
                                        {
                                            InitialTOT = (eo.initialTOT == default(DateTime)) ? eo.TOT_JST : eo.initialTOT;
                                        }
                                        bytePerSec = eo.bytePerSec;
                                        trackBar_Pos.Tag = 1;
                                        trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / eo.bytePerSec);
                                        trackBar_Pos.Value = (int)(((SkipByte ?? 0) + eo.Position) / eo.bytePerSec);
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
                            SeekUDPtoPos = TimeSpan.FromDays(100);
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
                        if (SeekUDPtoPos < TimeSpan.FromDays(30))
                        {
                            SkipByte = (long)(SeekUDPtoPos.TotalSeconds * bytePerSec * 0.9);
                            if (SkipByte > downitem.contentProperties.size)
                                break;
                            continue;
                        }
                        SeekUDPtoPos = TimeSpan.FromDays(100);
                        nextUDPcount--;
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
                catch
                {
                    try
                    {
                        SendDuration = TimeSpan.Parse(textBox_Duration.Text);
                    }
                    catch
                    {
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
                if (SeekUDPtoPos < TimeSpan.FromDays(30))
                {
                    trackBar_Pos.Tag = 1;
                    trackBar_Pos.Value = (int)SeekUDPtoPos.TotalSeconds;
                    trackBar_Pos.Tag = 0;
                }
            }
            else
            {
                timer1.Enabled = false;
                SeekUDPtoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
                label_stream.Text = string.Format(
                    "seeking to {0}",
                    SeekUDPtoPos.ToString());
                timer1.Enabled = true;
            }
        }

        private void trackBar_Pos_MouseCaptureChanged(object sender, EventArgs e)
        {
            SeekUDPtoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
            timer1.Enabled = false;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            SeekUDPtoPos = TimeSpan.FromSeconds(trackBar_Pos.Value);
            timer1.Enabled = false;
            CancelForSeekUDP();
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            SeekUDPtoPos = TimeSpan.FromDays(100);
            nextUDPcount++;
            CancelForSeekUDP();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        /// 
        /// play files with ffmodule(FFmpeg)
        /// 
        ////////////////////////////////////////////////////////////////////////////////////////////////

        ffmodule.FFplayer ffplayer = null;

        private async Task PlayWithFFmpeg()
        {
            await CancelTask("play");
            using (var Player = new ffmodule.FFplayer())
            using (var logger = Stream.Synchronized(new LogWindowStream(Config.Log)))
            using (var logwriter = TextWriter.Synchronized(new StreamWriter(logger)))
            {
                ffplayer = Player;
                try
                {
                    Player.GetImageFunc = new ffmodule.GetImageDelegate(GetImage);
                    Player.Fullscreen = Config.FFmodule_fullscreen;
                    Player.Display = Config.FFmodule_display;
                    Player.FontPath = Config.FontFilepath;
                    Player.FontSize = Config.FontPtSize;
                    Player.Volume = Config.FFmodule_volume;
                    Player.Mute = Config.FFmodule_mute;
                    Player.ScreenWidth = Config.FFmodule_width;
                    Player.ScreenHeight = Config.FFmodule_hight;
                    Player.ScreenXPos = Config.FFmodule_x;
                    Player.ScreenYPos = Config.FFmodule_y;
                    Player.ScreenAuto = Config.FFmodule_AutoResize;
                    Player.SetKeyFunctions(Config.FFmoduleKeybinds.Cast<dynamic>().ToDictionary(entry => (ffmodule.FFplayerKeymapFunction)entry.Key, entry => ((FFmoduleKeysClass)entry.Value).Cast<Keys>().ToArray()));
                    ffmodule.FFplayer.SetLogger(logwriter);
                    await PlayFiles(PlayOneFFmpegPlayer, "FFmpeg", data: Player);
                    ffmodule.FFplayer.SetLogger(null);
                }
                finally
                {
                    Config.FFmodule_fullscreen = Player.Fullscreen;
                    Config.FFmodule_display = Player.Display;
                    Config.FFmodule_mute = Player.Mute;
                    Config.FFmodule_volume = Player.Volume;
                    Config.FFmodule_width = Player.ScreenWidth;
                    Config.FFmodule_hight = Player.ScreenHeight;
                    Config.FFmodule_x = Player.ScreenXPos;
                    Config.FFmodule_y = Player.ScreenYPos;
                    ffplayer = null;
                }
            }
        }

        private async void button_FFplay_Click(object sender, EventArgs e)
        {
            await PlayWithFFmpeg();
        }

        private async void playWithFFplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await PlayWithFFmpeg();
        }

        private Bitmap GetImage(ffmodule.FFplayer player)
        {
            try
            {
                Bitmap ret = null;
                FileMetadata_Info downitem = player.Tag as FileMetadata_Info;
                ImageCodecInfo[] decoders = ImageCodecInfo.GetImageDecoders();
                string filename = downitem.name;
                var target = DriveTree[downitem.parents[0]].children.Where(x => x.Value.info.name.StartsWith(Path.GetFileNameWithoutExtension(filename)));
                foreach (var t in target)
                {
                    var ext = Path.GetExtension(t.Value.info.name).ToLower();
                    foreach (var ici in decoders)
                    {
                        bool found = false;
                        var decext = ici.FilenameExtension.Split(';').Select(x => Path.GetExtension(x).ToLower()).ToArray();
                        if (decext.Contains(ext))
                        {
                            Drive.downloadFile(t.Value.info.id, ct: player.ct).ContinueWith(task =>
                            {
                                var img = Image.FromStream(task.Result);
                                ret = new Bitmap(img);
                                found = true;
                            }).Wait();
                            if (found) return ret;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        double FFplayStartDelay = double.NaN;
        double FFplayDuration = double.NaN;

        private async Task PlayOneFFmpegPlayer(FileMetadata_Info downitem, string download_str, CancellationToken ct, object data)
        {
            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
            toolStripProgressBar1.Maximum = 10000;
            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
            toolStripStatusLabel1.Text = download_str + " " + downitem.name;
            label_FFplay_sendname.Text = downitem.name;
            var Player = data as ffmodule.FFplayer;
            timer3.Enabled = true;
            Player.StartSkip = FFplayStartDelay;
            Player.StopDuration = FFplayDuration;
            await Task.Run(() =>
            {
                using (var driveStream = new AmazonDriveStream(Drive, downitem))
                using (var PosStream = new PositionStream(driveStream))
                {
                    PosStream.PosChangeEvent += (src, evnt) =>
                    {
                        synchronizationContext.Post(
                            (o) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                var eo = o as PositionChangeEventArgs;
                                toolStripStatusLabel1.Text = download_str + eo.Log + " " + downitem.name;
                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                toolStripProgressBar1.Maximum = 10000;
                                toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                            }, evnt);
                    };
                    Player.Tag = downitem;
                    if (Player.Play(PosStream, downitem.name, ct) != 0)
                        throw new OperationCanceledException("player cancel");
                }
            }, ct);
            timer3.Enabled = false;
            label_FFplay_sendname.Text = "Play Filename";
        }

        private void button_FFplay_next_Click(object sender, EventArgs e)
        {
            ffplayer?.Stop();
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            try
            {
                trackBar_FFplay_pos.Maximum = 10000;
                var value = (int)((ffplayer?.PlayTime ?? 0) / (ffplayer?.Duration ?? 1) * trackBar_FFplay_pos.Maximum);
                trackBar_FFplay_pos.Tag = 1;
                trackBar_FFplay_pos.Value = (value < trackBar_FFplay_pos.Minimum) ? 0 : (value > trackBar_FFplay_pos.Maximum) ? trackBar_FFplay_pos.Maximum : value;
                trackBar_FFplay_pos.Tag = 0;
                label_FFplay_stream.Text = string.Format("{0} / {1}",
                    TimeSpan.FromSeconds(ffplayer?.PlayTime ?? 0).ToString(@"hh\:mm\:ss\.fff"),
                    TimeSpan.FromSeconds(ffplayer?.Duration ?? 0).ToString(@"hh\:mm\:ss"));
            }
            catch { }
        }

        private void trackBar_FFplay_pos_MouseCaptureChanged(object sender, EventArgs e)
        {
            timer3.Enabled = false;
            timer4.Enabled = true;
        }

        private void trackBar_FFplay_pos_ValueChanged(object sender, EventArgs e)
        {
            if (trackBar_FFplay_pos.Tag as int? == 1) return;
            timer4.Enabled = false;
            timer3.Enabled = false;
            label_FFplay_stream.Text = string.Format("seek to {0} / {1}",
                TimeSpan.FromSeconds((double)trackBar_FFplay_pos.Value / trackBar_FFplay_pos.Maximum * (ffplayer?.Duration ?? 1)).ToString(@"hh\:mm\:ss\.fff"),
                TimeSpan.FromSeconds(ffplayer?.Duration ?? 0).ToString(@"hh\:mm\:ss"));
            timer4.Enabled = true;
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            timer4.Enabled = false;
            if (ffplayer != null)
            {
                var val = (double)trackBar_FFplay_pos.Value / trackBar_FFplay_pos.Maximum * (ffplayer?.Duration ?? 1);
                ffplayer.PlayTime = val;
                timer3.Enabled = true;
            }
        }

        private void textBox_FFplayStart_Leave(object sender, EventArgs e)
        {
            if (textBox_FFplayStart.Text == "")
                FFplayStartDelay = double.NaN;
            else
            {
                try
                {
                    FFplayStartDelay = double.Parse(textBox_FFplayStart.Text);
                }
                catch
                {
                    try
                    {
                        FFplayStartDelay = TimeSpan.Parse(textBox_FFplayStart.Text).TotalSeconds;
                    }
                    catch
                    {
                        FFplayStartDelay = double.NaN;
                    }
                }
            }
            textBox_FFplayStart.Text = (double.IsNaN(FFplayStartDelay)) ? "" : TimeSpan.FromSeconds(FFplayStartDelay).ToString();
        }

        private void textBox_FFplayDuration_Leave(object sender, EventArgs e)
        {
            if (textBox_FFplayDuration.Text == "")
                FFplayDuration = double.NaN;
            else
            {
                try
                {
                    FFplayDuration = double.Parse(textBox_FFplayDuration.Text);
                }
                catch
                {
                    try
                    {
                        FFplayDuration = TimeSpan.Parse(textBox_FFplayDuration.Text).TotalSeconds;
                    }
                    catch
                    {
                        FFplayDuration = double.NaN;
                    }
                }
            }
            textBox_FFplayDuration.Text = (double.IsNaN(FFplayDuration)) ? "" : TimeSpan.FromSeconds(FFplayDuration).ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////
        /// 
        /// play files with given method(SendUDP, FFplay)
        /// 
        ////////////////////////////////////////////////////////////////////////////////////////////////

        private delegate Task PlayOneFileDelegate(FileMetadata_Info downitem, string download_str, CancellationToken ct, object data);

        private async Task PlayFiles(PlayOneFileDelegate func, string LogPrefix, CancellationToken ct = default(CancellationToken), object data = null)
        {
            Config.Log.LogOut(LogPrefix + " media files Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedItems;
            if (select.Count == 0) return;

            var selectItem = select.OfType<ListViewItem>().Select(x => (x.Tag as ItemInfo).info).Where(x => x.kind != "FOLDER").ToArray();

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            int f_cur = 0;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = CreateTask("play");
                ct = task.cts.Token;
            }
            try
            {
                nextUDPcount = 0;
                foreach (var downitem in selectItem)
                {
                    ct.ThrowIfCancellationRequested();
                    Config.Log.LogOut(LogPrefix + " download : " + downitem.name);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    if (downitem.contentProperties.size > ConfigAPI.FilenameChangeTrickSize)
                    {
                        Config.Log.LogOut(LogPrefix + " download : <BIG FILE> temporary filename change");
                        Interlocked.Increment(ref CriticalCount);
                        try
                        {
                            toolStripStatusLabel1.Text = "temporary filename change...";
                            var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id);
                            try
                            {
                                await func(downitem, download_str, ct, data);
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
                        await func(downitem, download_str, ct, data);
                    }

                    Config.Log.LogOut(LogPrefix + " download : done.");
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
                Config.Log.LogOut(LogPrefix + " download : Error");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show(LogPrefix + " : ERROR\r\n" + ex.Message);
            }
            finally
            {
                FinishTask(task);
            }
            label_sendname.Text = "Send Filename";
            label_FFplay_sendname.Text = "Play Filename";
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////

        private double ConvertUnit(double value, string Unit)
        {
            switch (Unit)
            {
                case "GiB/s":
                    return value * 1024 * 1024 * 1024;
                case "MiB/s":
                    return value * 1024 * 1024;
                case "KiB/s":
                    return value * 1024;
                case "GB/s":
                    return value * 1000 * 1000 * 1000;
                case "MB/s":
                    return value * 1000 * 1000;
                case "KB/s":
                    return value * 1000;
            }
            return value;
        }

        private void textBox_UploadBandwidhtLimit_TextChanged(object sender, EventArgs e)
        {
            if (comboBox_UploadLimitUnit.SelectedIndex == comboBox_UploadLimitUnit.Items.IndexOf("Infinity"))
            {
                Config.UploadLimit = double.PositiveInfinity;
                textBox_UploadBandwidthLimit.Text = "";
            }
            try
            {
                double value = double.Parse(textBox_UploadBandwidthLimit.Text);
                Config.UploadLimit = ConvertUnit(value, (string)comboBox_UploadLimitUnit.SelectedItem);
            }
            catch { }
        }

        private void textBox_DownloadBandwidthLimit_TextChanged(object sender, EventArgs e)
        {
            if (comboBox_DownloadLimitUnit.SelectedIndex == comboBox_DownloadLimitUnit.Items.IndexOf("Infinity"))
            {
                Config.DownloadLimit = double.PositiveInfinity;
                textBox_DownloadBandwidthLimit.Text = "";
            }
            try
            {
                double value = double.Parse(textBox_DownloadBandwidthLimit.Text);
                Config.DownloadLimit = ConvertUnit(value, (string)comboBox_DownloadLimitUnit.SelectedItem);
            }
            catch { }
        }

        private void comboBox_UploadLimitUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_UploadLimitUnit.SelectedIndex == comboBox_UploadLimitUnit.Items.IndexOf("Infinity"))
            {
                Config.UploadLimit = double.PositiveInfinity;
                textBox_UploadBandwidthLimit.Text = "";
            }
            try
            {
                double value = double.Parse(textBox_UploadBandwidthLimit.Text);
                Config.UploadLimit = ConvertUnit(value, (string)comboBox_UploadLimitUnit.SelectedItem);
            }
            catch { }
        }

        private void comboBox_DownloadLimitUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_DownloadLimitUnit.SelectedIndex == comboBox_DownloadLimitUnit.Items.IndexOf("Infinity"))
            {
                Config.DownloadLimit = double.PositiveInfinity;
                textBox_DownloadBandwidthLimit.Text = "";
            }
            try
            {
                double value = double.Parse(textBox_DownloadBandwidthLimit.Text);
                Config.DownloadLimit = ConvertUnit(value, (string)comboBox_DownloadLimitUnit.SelectedItem);
            }
            catch { }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private void button_breakone_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView_TaskList.SelectedItems)
                (item.Tag as TaskCanselToken).cts.Cancel();
        }

        private void button_break_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView_TaskList.Items)
                (item.Tag as TaskCanselToken).cts.Cancel();
        }

        private void buttonFFmpegmoduleConfig_Click(object sender, EventArgs e)
        {
            var form = new FormFFmoduleConfig();
            form.ShowDialog();
        }

    }


    public class ItemInfo
    {
        public FileMetadata_Info info;
        public TreeNode tree;
        public Dictionary<string, ItemInfo> children = new Dictionary<string, ItemInfo>();

        public ItemInfo(FileMetadata_Info thisdata)
        {
            info = thisdata;
        }
    }
}

