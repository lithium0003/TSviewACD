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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
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

        public TaskCanselToken CreateTask(string taskname)
        {
            var item = new ListViewItem(taskname);
            var task = new TaskCanselToken(taskname);
            item.Tag = task;
            Invoke(new Action(() => { listView_TaskList.Items.Add(item); }));
            return task;
        }

        public void FinishTask(TaskCanselToken task)
        {
            Invoke(new Action(() =>
            {
                var removes = listView_TaskList.Items.OfType<ListViewItem>().Where(x => x.Tag == task).ToArray();
                if (removes.Count() > 0)
                {
                    foreach (var item in removes)
                        listView_TaskList.Items.Remove(item);
                }
            }));
        }

        public async Task CancelTask(string taskname)
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

        public bool CancelTaskAll()
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
            if ((int)listviewitem.SortColum == e.Column)
            {
                if (listviewitem.SortOrder == SortOrder.Ascending)
                    listviewitem.SortOrder = SortOrder.Descending;
                else
                    listviewitem.SortOrder = SortOrder.Ascending;
            }
            else
            {
                listviewitem.SortColum = (ListColums)e.Column;
                listviewitem.SortOrder = SortOrder.Ascending;
            }
            listView1.Refresh();
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
        bool initialized = false;
        bool supressListviewRefresh = false;

        AmazonDrive Drive = DriveData.Drive;

        enum ListColums
        {
            Name = 0,
            Size = 1,
            modifiedDate = 2,
            createdDate = 3,
            path = 4,
            id = 5,
            MD5 = 6,
        };

        class AmazonListViewItem
        {
            private ItemInfo[] _Items = new ItemInfo[0];
            private ItemInfo _Parent = null;
            private ItemInfo _Root = null;
            private ListColums _SortColum = ListColums.Name;
            private SortOrder _SortOrder = System.Windows.Forms.SortOrder.Ascending;
            private bool _SortKind = false;

            private Func<IEnumerable<ItemInfo>, IOrderedEnumerable<ItemInfo>> SortFunction
            {
                get
                {
                    if (_SortOrder != SortOrder.Descending)
                    {
                        switch (_SortColum)
                        {
                            case ListColums.Name:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => (Config.AutoDecode)? y.DisplayName: y.info.name);
                            case ListColums.Size:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.info.contentProperties?.size ?? 0);
                            case ListColums.modifiedDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.info.modifiedDate);
                            case ListColums.createdDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.info.createdDate);
                            case ListColums.path:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => DriveData.GetFullPathfromItem(y));
                            case ListColums.MD5:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.info.contentProperties?.md5 ?? "");
                            default:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => (Config.AutoDecode) ? y.DisplayName : y.info.name);
                        }
                    }
                    else
                    {
                        switch (_SortColum)
                        {
                            case ListColums.Name:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => (Config.AutoDecode) ? y.DisplayName : y.info.name);
                            case ListColums.Size:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.info.contentProperties?.size ?? 0);
                            case ListColums.modifiedDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.info.modifiedDate);
                            case ListColums.createdDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.info.createdDate);
                            case ListColums.path:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => DriveData.GetFullPathfromItem(y));
                            case ListColums.MD5:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.info.contentProperties?.md5 ?? "");
                            default:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => (Config.AutoDecode) ? y.DisplayName : y.info.name);
                        }
                    }
                }
            }

            private void Sort()
            {
                _Items = SortFunction(_Items).ToArray();
            }

            public ListColums SortColum
            {
                get { return _SortColum; }
                set
                {
                    _SortColum = value;
                    Sort();
                }
            }
            public SortOrder SortOrder
            {
                get { return _SortOrder; }
                set
                {
                    _SortOrder = value;
                    Sort();
                }
            }
            public bool SortKind
            {
                get { return _SortKind; }
                set
                {
                    _SortKind = value;
                    Sort();
                }
            }
            public bool IsSearchResult
            {
                get; private set;
            }

            public ItemInfo[] SearchResult
            {
                get { return _Items; }
                set
                {
                    _Root = null;
                    _Parent = null;
                    _Items = SortFunction(value).ToArray();
                    IsSearchResult = true;
                }
            }

            public IEnumerable<ItemInfo> GetItems(ListView.SelectedIndexCollection indices, bool IncludeSpetial = true)
            {
                List<ItemInfo> ret = new List<ItemInfo>();
                foreach(int i in indices)
                {
                    if(Root == null)
                    {
                        if (i >= 0 && i < Items.Length) ret.Add(Items[i]);
                    }
                    else
                    {
                        if (i == 0)
                        {
                            if (IncludeSpetial) ret.Add(Root);
                        }
                        else if (i == 1)
                        {
                            if (IncludeSpetial) ret.Add(Parent);
                        }
                        else if (i >= 2 && i - 2 < Items.Length) ret.Add(Items[i - 2]);
                    }
                }
                return ret;
            }

            public void Clear()
            {
                Root = null;
            }

            public ItemInfo[] Items
            {
                get { return _Items; }
            }
            public ItemInfo Parent
            {
                get { return _Parent; }
            }
            public ItemInfo Root
            {
                get { return _Root; }
                set
                {
                    IsSearchResult = false;
                    if(value == null)
                    {
                        _Root = null;
                        _Parent = null;
                        _Items = new ItemInfo[0];
                    }
                    else
                    {
                        _Root = value;
                        if(_Root.info.id == DriveData.AmazonDriveRootID)
                        {
                            _Parent = _Root;
                        }
                        else
                        {
                            _Parent = DriveData.AmazonDriveTree[_Root.info.parents[0]];
                        }
                        _Items = SortFunction(_Root.children.Values).ToArray();
                    }
                }
            }
            public int Count
            {
                get { return (_Root == null) ? _Items.Length : _Items.Length + 2; }
            }


            private ListViewItem ConvertNormalItem(ItemInfo x)
            {
                var item = new ListViewItem(
                    new string[] {
                            (Config.AutoDecode)? x.DisplayName: x.info.name,
                            x.info.contentProperties?.size?.ToString("#,0"),
                            x.info.modifiedDate.ToString(),
                            x.info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(x),
                            x.info.id,
                            x.info.contentProperties?.md5,
                    }, (x.info.kind == "FOLDER") ? 0 : 2);
                item.Name = (Config.AutoDecode) ? x.DisplayName : x.info.name;
                item.Tag = x;
                item.ToolTipText = item.Name;
                switch (x.IsEncrypted)
                {
                    case CryptMethods.Method0_Plain:
                        break;
                    case CryptMethods.Method1_CTR:
                        item.ForeColor = Color.Blue;
                        if (x.CryptError)
                        {
                            item.BackColor = Color.LightPink;
                        }
                        break;
                    case CryptMethods.Method2_CBC_CarotDAV:
                        item.ForeColor = Color.ForestGreen;
                        if (x.CryptError)
                        {
                            item.BackColor = Color.LightPink;
                        }
                        break;
                }
                return item;
            }

            public ListViewItem this[int index]
            {
                get
                {
                    if(_Root == null)
                    {
                        if (index < Items.Length)
                        {
                            return ConvertNormalItem(Items[index]);
                        }
                        else
                            return new ListViewItem(new string[7]);
                    }
                    if (index == 0)
                    {
                        var root = Root;
                        var rootitem = new ListViewItem(
                            new string[] {
                            ".",
                            "",
                            root.info.modifiedDate.ToString(),
                            root.info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(root),
                            root.info.id,
                            "",
                            }, 0);
                        rootitem.Tag = root;
                        rootitem.Name = (root.info.id == DriveData.AmazonDriveRootID) ? "/" : ".";
                        rootitem.ToolTipText = Resource_text.CurrentFolder_str;
                        return rootitem;
                    }
                    if(index == 1)
                    {
                        var up = Parent;
                        var upitem = new ListViewItem(
                            new string[] {
                            "..",
                            "",
                            up.info.modifiedDate.ToString(),
                            up.info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(up),
                            up.info.id,
                            "",
                            }, 0);
                        upitem.Tag = up;
                        upitem.Name = (up.info.id == DriveData.AmazonDriveRootID) ? "/" : "..";
                        upitem.ToolTipText = Resource_text.UpFolder_str;
                        return upitem;
                    }
                    if (index > 1 && index - 2 < Items.Length)
                    {
                        return ConvertNormalItem(Items[index - 2]);
                    }
                    else
                        return new ListViewItem(new string[7]);
                }
            }
            public bool Contains(string id)
            {
                return (Root?.info.id == id) || (Parent?.info.id == id) || (Items.Select(x => x.info.id).Contains(id));
            }
            public bool Contains(IEnumerable<string> id)
            {
                return id.Select(x => Contains(x)).Any();
            }
        }

        AmazonListViewItem listviewitem = new AmazonListViewItem();

        private async Task Login()
        {
            Config.Log.LogOut("Login Start.");
            var task = TaskCanceler.CreateTask("login");
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
                TaskCanceler.FinishTask(task);
            }
        }

        private async Task Logout()
        {
            Config.Log.LogOut("Logout Start.");
            if (TaskCanceler.CancelTaskAll())
            {
                while (listView_TaskList.Items.Count > 0)
                    await Task.Delay(100);
            }
            initialized = false;
            DriveData.RemoveCache();
            DriveData.Drive = new AmazonDrive();
            Drive = DriveData.Drive;
            Config.refresh_token = "";
            Config.Save();
            treeView1.Nodes.Clear();
            listviewitem.Clear();
            listView1.VirtualListSize = listviewitem.Count;
            loginToolStripMenuItem.Enabled = true;
            toolStripMenuItem_Logout.Enabled = false;
        }



        private async Task InitAlltree()
        {
            var task = TaskCanceler.CreateTask("Init drive tree data");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading treedata...";
                await DriveData.InitDrive(ct: ct,
                    inprogress: (str) =>
                    {
                        toolStripStatusLabel1.Text = str;
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    },
                    done: (str) =>
                    {
                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripStatusLabel1.Text = str;
                    });
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
                TaskCanceler.FinishTask(task);
            }
        }

        private TreeNode[] GenerateTreeNode(IEnumerable<ItemInfo> children, int count = 0)
        {
            var ret = new List<TreeNode>();
            Parallel.ForEach(children, ()=> new List<TreeNode>(), (x, state, local) =>
            {
                int img = (x.info.kind == "FOLDER") ? 0 : 2;
                var node = new TreeNode((Config.AutoDecode) ? x.DisplayName : x.info.name, img, img);
                node.Name = (Config.AutoDecode) ? x.DisplayName : x.info.name;
                node.Tag = x;
                switch (x.IsEncrypted)
                {
                    case CryptMethods.Method0_Plain:
                        break;
                    case CryptMethods.Method1_CTR:
                        node.ForeColor = Color.Blue;
                        break;
                    case CryptMethods.Method2_CBC_CarotDAV:
                        node.ForeColor = Color.ForestGreen;
                        break;
                }
                if (x.info.kind == "FOLDER" && count > 0 && x.children.Count > 0)
                {
                    node.Nodes.AddRange(GenerateTreeNode(x.children.Values, count - 1));
                }
                ItemInfo value;
                if (DriveData.AmazonDriveTree.TryGetValue(x.info.id, out value))
                {
                    value.tree = node;
                }
                else
                {
                    DriveData.AmazonDriveTree[x.info.id] = new ItemInfo(null);
                    DriveData.AmazonDriveTree[x.info.id].tree = node;
                }
                local.Add(node);
                return local;
            },
            (result)=>
            {
                lock(ret)
                    ret.AddRange(result);
            }
            );
            return ret.ToArray();
        }

        private async Task InitView()
        {
            // Load Drive Tree
            await InitAlltree();

            // Refresh Drive Tree
            await ReloadItems(DriveData.AmazonDriveRootID, false);
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

        private void FollowPath(string path_str)
        {
            string target_id = DriveData.PathToID(path_str);

            if (target_id != DriveData.AmazonDriveRootID)
            {
                if (DriveData.AmazonDriveTree[target_id].tree == null)
                {
                    // not loaded tree
                    List<string> tree_ids = new List<string>();
                    tree_ids.Add(target_id);
                    var p = DriveData.AmazonDriveTree[target_id].info.parents[0];
                    while (DriveData.AmazonDriveTree[p].tree == null)
                    {
                        tree_ids.Add(p);
                        p = DriveData.AmazonDriveTree[p].info.parents[0];
                    }
                    tree_ids.Reverse();
                    DriveData.AmazonDriveTree[p].tree.Nodes.AddRange(GenerateTreeNode(DriveData.AmazonDriveTree[p].children.Values));
                    foreach (var t in tree_ids)
                    {
                        DriveData.AmazonDriveTree[t].tree.Nodes.AddRange(GenerateTreeNode(DriveData.AmazonDriveTree[t].children.Values));
                    }
                }
                treeView1.SelectedNode = DriveData.AmazonDriveTree[target_id].tree;
                treeView1.SelectedNode.Expand();
            }

            //// display listview Root
            listviewitem.Root = DriveData.AmazonDriveTree[target_id];
            listView1.VirtualListSize = listviewitem.Count;
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
            textBox_Password.Text = Config.DrivePassword;
            checkBox_crypt.Checked = Config.UseEncryption;
            checkBox_cryptfilename.Checked = Config.UseFilenameEncryption;
            checkBox_LockPassword.Checked = Config.LockPassword;
            switch (Config.Language)
            {
                case "en":
                    englishToolStripMenuItem.Checked = true;
                    break;
                case "ja":
                    japaneseToolStripMenuItem.Checked = true;
                    break;
                case "":
                    defaultToolStripMenuItem.Checked = true;
                    break;
            }
            switch (Config.CryptMethod)
            {
                case CryptMethods.Method1_CTR:
                    radioButton_crypt_1_CTR.Checked = true;
                    break;
                case CryptMethods.Method2_CBC_CarotDAV:
                    radioButton_crypt_2_CBC.Checked = true;
                    break;
            }
            checkBox_decodeView.Checked = Config.AutoDecode;
            comboBox_CarotDAV_Escape.Items.AddRange(Config.CarotDAV_crypt_names);
            comboBox_CarotDAV_Escape.Text = Config.CarotDAV_CryptNameHeader;
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
            if (TaskCanceler.CancelTaskAll() || Config.AmazonDriveTempCount > 0)
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
                    listviewitem.Root = DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID];
                    listView1.VirtualListSize = listviewitem.Count;
                    return;
                }

                var selectdata = e.Node.Tag as ItemInfo;
                if (selectdata == null) return;

                if (selectdata.info.kind == "FOLDER")
                {
                    listviewitem.Root = selectdata;
                    listView1.VirtualListSize = listviewitem.Count;
                }
                else
                {
                    listviewitem.Root = DriveData.AmazonDriveTree[selectdata.info.parents[0]];
                    listView1.VirtualListSize = listviewitem.Count;
                }
            }
        }

        private void largeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.LargeIcon;
            largeIconToolStripMenuItem.Checked = true;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
        }

        private void smallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.SmallIcon;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = true;
            listToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = false;
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.List;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = true;
            detailToolStripMenuItem.Checked = false;
        }

        private void detailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem.Checked = false;
            detailToolStripMenuItem.Checked = true;
        }

        private async void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            var selectdata = listviewitem.GetItems(listView1.SelectedIndices).FirstOrDefault();

            if (selectdata == null) return;
            if (selectdata.info.kind == "FOLDER")
            {
                listviewitem.Root = selectdata;
                listView1.VirtualListSize = listviewitem.Count;
                listView1.SelectedIndices.Clear();
                listView1.Refresh();
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_SendUDP")
            {
                await TaskCanceler.CancelTask("play");
                await PlayFiles(PlayOneTSFile, "Send UDP");
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_FFmpeg")
            {
                await PlayWithFFmpeg();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            if (listView1.SelectedIndices.Count > 1) return;
            var selectdata = listviewitem.GetItems(listView1.SelectedIndices).FirstOrDefault();
            var tree = selectdata?.tree;
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
            if (!initialized) return;
            FollowPath(textBox_path.Text);
        }

        public async Task<int> DoFileUpload(IEnumerable<string> Filenames, string parent_id, int f_all = 1, int f_cur = 0, CancellationToken ct = default(CancellationToken))
        {
            FileMetadata_Info[] done_files = null;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("FileUpload");
                ct = task.cts.Token;
            }
            try
            {
                if (checkBox_upSkip.Checked)
                {
                    using(await DriveData.DriveLock.LockAsync())
                    {
                        done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                    }
                }

                foreach (var filename in Filenames)
                {
                    ct.ThrowIfCancellationRequested();
                    Config.Log.LogOut("Upload File: " + filename);
                    var error = await Task.Run(async () =>
                    {
                        var upload_str = (f_all > 1) ? string.Format("Upload({0}/{1})...", ++f_cur, f_all) : "Upload...";
                        var short_filename = System.IO.Path.GetFileName(filename);
                        var enckey = short_filename + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        string uploadfilename = short_filename;
                        if (Config.UseEncryption)
                        {
                            if (Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                if (Config.UseFilenameEncryption)
                                    uploadfilename = Path.GetRandomFileName();
                                else
                                    uploadfilename = enckey + ".enc";
                            }
                            else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                uploadfilename = CryptCarotDAV.EncryptFilename(short_filename);
                                enckey = "";
                            }
                        }
                        var checkpoint = DriveData.ChangeCheckpoint;
                        var filesize = new FileInfo(filename).Length;
                        if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            filesize = filesize + CryptCarotDAV.BlockSizeByte + CryptCarotDAV.CryptHeaderByte + CryptCarotDAV.CryptFooterByte;
                        }

                        bool dup_flg = done_files?.Select(x => x.name.ToLower()).Contains(short_filename.ToLower()) ?? false;
                        if (Config.UseEncryption)
                        {
                            if (Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                dup_flg = dup_flg || (done_files?.Select(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename)).Any(x => x) ?? false);
                                if (Config.UseFilenameEncryption)
                                {
                                    dup_flg = dup_flg || (done_files?.Select(x =>
                                    {
                                        var enc = DriveData.DecryptFilename(x);
                                        if (enc == null) return false;
                                        return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                    }).Any(x => x) ?? false);
                                }
                            }
                            if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                dup_flg = dup_flg || (done_files?.Select(x =>
                                {
                                    var enc = CryptCarotDAV.DecryptFilename(x.name);
                                    if (enc == null) return false;
                                    return enc == short_filename;
                                }).Any(x => x) ?? false);
                            }
                        }

                        if (dup_flg)
                        {
                            var target = done_files.FirstOrDefault(x => x.name == short_filename);
                            if (target == null && Config.UseEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                target = done_files.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename)).FirstOrDefault();
                                if (target == null && Config.UseFilenameEncryption)
                                {
                                    target = done_files.Where(x =>
                                    {
                                        var enc = DriveData.DecryptFilename(x);
                                        if (enc == null) return false;
                                        return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                    }).FirstOrDefault();
                                }
                            }
                            if (target == null && Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                target = done_files.Where(x =>
                                {
                                    var enc = CryptCarotDAV.DecryptFilename(x.name);
                                    if (enc == null) return false;
                                    return enc == short_filename;
                                }).FirstOrDefault();
                            }

                            if (filesize == target?.contentProperties?.size)
                            {
                                if (!checkBox_MD5.Checked)
                                    return false;

                                using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                                using (var hfile = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    byte[] md5 = null;
                                    synchronizationContext.Post(
                                        (txt) =>
                                        {
                                            if (ct.IsCancellationRequested) return;
                                            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                                            toolStripStatusLabel1.Text = txt as string;
                                        }, upload_str + " " + short_filename + " Check file MD5...");
                                    if (Config.UseEncryption)
                                    {
                                        if (Config.CryptMethod == CryptMethods.Method1_CTR)
                                        {
                                            string nonce = null;
                                            if (Config.UseFilenameEncryption)
                                            {
                                                nonce = DriveData.DecryptFilename(target);
                                            }
                                            if (Path.GetExtension(target.name) == ".enc")
                                            {
                                                nonce = Path.GetFileNameWithoutExtension(target.name);
                                            }
                                            if (!string.IsNullOrEmpty(nonce))
                                                using (var encfile = new CryptCTR.AES256CTR_CryptStream(hfile, nonce))
                                                {
                                                    md5 = md5calc.ComputeHash(encfile);
                                                }
                                        }
                                        else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                        {
                                            using (var encfile = new CryptCarotDAV.CryptCarotDAV_CryptStream(hfile))
                                            {
                                                md5 = md5calc.ComputeHash(encfile);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        md5 = md5calc.ComputeHash(hfile);
                                    }
                                    synchronizationContext.Post(
                                        (txt) =>
                                        {
                                            if (ct.IsCancellationRequested) return;
                                            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                            toolStripStatusLabel1.Text = txt as string;
                                        }, "Check done.");
                                    if (BitConverter.ToString(md5).ToLower().Replace("-", "") == target.contentProperties?.md5)
                                        return false;
                                }
                            }
                            Config.Log.LogOut(string.Format("conflict. name:{0} upload:{1}", short_filename, uploadfilename));
                            if (!checkBox_overrideUpload.Checked)
                                return false;
                            Config.Log.LogOut("remove item...");
                            try
                            {
                                checkpoint = DriveData.ChangeCheckpoint;
                                foreach (var conflicts in done_files.Where(x => x.name.ToLower() == short_filename.ToLower()))
                                {
                                    await Drive.TrashItem(conflicts.id, ct);
                                }
                                if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                {
                                    var conflict_crypt = done_files.Where(x =>
                                    {
                                        var enc = CryptCarotDAV.DecryptFilename(x.name);
                                        if (enc == null) return false;
                                        return enc == short_filename;
                                    });
                                    foreach (var conflicts in conflict_crypt)
                                    {
                                        await Drive.TrashItem(conflicts.id, ct);
                                    }
                                }
                                if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                                {
                                    if (Config.UseFilenameEncryption)
                                    {
                                        var conflict_crypt = done_files.Where(x =>
                                        {
                                            var enc = DriveData.DecryptFilename(x);
                                            if (enc == null) return false;
                                            return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                        });
                                        foreach (var conflicts in conflict_crypt)
                                        {
                                            await Drive.TrashItem(conflicts.id, ct);
                                        }
                                    }
                                    else
                                    {
                                        var conflict_crypt = done_files.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename));
                                        foreach (var conflicts in conflict_crypt)
                                        {
                                            await Drive.TrashItem(conflicts.id, ct);
                                        }
                                    }
                                }
                                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                                await DriveData.GetChanges(checkpoint, ct);
                                using (await DriveData.DriveLock.LockAsync())
                                {
                                    done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                                }
                                checkpoint = DriveData.ChangeCheckpoint;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception)
                            {
                            }
                        }

                        synchronizationContext.Post(
                            (txt) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                                toolStripProgressBar1.Maximum = 10000;
                                toolStripStatusLabel1.Text = txt as string;
                            }, upload_str + " " + short_filename);

                        int retry = 6;
                        while (--retry > 0)
                        {
                            int checkretry = 4;
                            string uphash = null;
                            try
                            {
                                var ret = await Drive.uploadFile(
                                    filename: filename,
                                    uploadname: uploadfilename,
                                    uploadkey: enckey,
                                    parent_id: parent_id,
                                    process: (src, evnt) =>
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
                                var tmpDone = done_files.ToList();
                                tmpDone.Add(ret);
                                done_files = tmpDone.ToArray();
                                break;
                            }
                            catch (AmazonDriveUploadException ex)
                            {
                                uphash = ex.Message;
                                if(ex.InnerException is HttpRequestException)
                                {
                                    if (ex.InnerException.Message.Contains("408 (REQUEST_TIMEOUT)")) checkretry = 6 * 5 + 1;
                                    if (ex.InnerException.Message.Contains("409 (Conflict)")) checkretry = 3;
                                    if (ex.InnerException.Message.Contains("504 (GATEWAY_TIMEOUT)")) checkretry = 6 * 5 + 1;
                                    if (filesize < Config.SmallFileSize) checkretry = 3;
                                }
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

                                    var children = await DriveData.GetChanges(checkpoint, ct);
                                    if (children.Where(x => x.name.Contains(uploadfilename)).LastOrDefault()?.status == "AVAILABLE")
                                    {
                                        Config.Log.LogOut("Upload : child found.");
                                        var uploadeditem = children.Where(x => x.name.Contains(uploadfilename)).LastOrDefault();
                                        var remotehash = uploadeditem?.contentProperties?.md5;
                                        if(uphash != remotehash)
                                        {
                                            Config.Log.LogOut(string.Format("Upload : but hash not match. upload:{0} remote:{1}", uphash, remotehash));
                                            checkretry = 0;
                                            await Drive.TrashItem(uploadeditem.id, ct);
                                            await Task.Delay(TimeSpan.FromSeconds(5), ct);
                                        }
                                        using (await DriveData.DriveLock.LockAsync())
                                        {
                                            done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                                        }
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
                            synchronizationContext.Post(
                                (txt) =>
                                {
                                    if (ct.IsCancellationRequested) return;
                                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                                    toolStripProgressBar1.Maximum = 100;
                                    toolStripStatusLabel1.Text = txt as string;
                                }, "Upload Failed.");
                            return true;
                        }

                        if (Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            Config.Log.LogOut("Encrypt Name.");
                            if (!await DriveData.EncryptFilename(uploadfilename: uploadfilename, enckey: enckey, checkpoint: checkpoint, ct: ct))
                            {
                                Config.Log.LogOut("Upload : failed.");
                                synchronizationContext.Post(
                                    (txt) =>
                                    {
                                        if (ct.IsCancellationRequested) return;
                                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                                        toolStripProgressBar1.Maximum = 100;
                                        toolStripStatusLabel1.Text = txt as string;
                                    }, "Upload Failed.");
                                return true;
                            }
                            using (await DriveData.DriveLock.LockAsync())
                            {
                                done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                            }
                        }

                        Config.Log.LogOut("Upload : done.");
                        synchronizationContext.Post(
                            (txt) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                                toolStripProgressBar1.Maximum = 100;
                                toolStripStatusLabel1.Text = txt as string;
                            }, "Upload done.");
                        return false;
                    }, ct);

                    if (error) return -1;
                }
                return f_cur;
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async Task<int> DoDirectoryUpload(IEnumerable<string> Filenames, string parent_id, int f_all, int f_cur, CancellationToken ct = default(CancellationToken))
        {
            FileMetadata_Info[] done_files = null;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("DirectoryUpload");
                ct = task.cts.Token;
            }
            try
            {
                if (checkBox_upSkip.Checked)
                {
                    using (await DriveData.DriveLock.LockAsync())
                    {
                        done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                    }
                }

                foreach (var filename in Filenames)
                {
                    ct.ThrowIfCancellationRequested();
                    var short_name = Path.GetFullPath(filename).Split(new char[] { '\\', '/' }).Last();

                    FileMetadata_Info newdir = null;
                    if (done_files?.Where(x => x.kind == "FOLDER").Select(x => x.name.ToLower()).Contains(short_name.ToLower()) ?? false)
                    {
                        newdir = done_files.First(x => x.name.ToLower() == short_name.ToLower() && x.kind == "FOLDER");
                    }
                    if (newdir == null && Config.UseEncryption)
                    {
                        if (Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            var selection = done_files?.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_name));
                            if (selection?.Any() ?? false)
                            {
                                newdir = selection.FirstOrDefault();
                            }
                            if (newdir == null && Config.UseFilenameEncryption)
                            {
                                selection = done_files?.Where(x =>
                                {
                                    var enc = DriveData.DecryptFilename(x);
                                    if (enc == null) return false;
                                    return Path.GetFileNameWithoutExtension(enc) == short_name;
                                });
                                if (selection?.Any() ?? false)
                                {
                                    newdir = selection.FirstOrDefault();
                                }
                            }
                        }
                        if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            var selection = done_files?.Where(x =>
                            {
                                var enc = CryptCarotDAV.DecryptFilename(x.name);
                                if (enc == null) return false;
                                return enc == short_name;
                            });
                            if (selection?.Any() ?? false)
                            {
                                newdir = selection.FirstOrDefault();
                            }
                        }
                    }
                    
                    if (newdir == null)
                    {
                        var enckey = short_name + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        var makedirname = short_name;

                        if (Config.UseEncryption)
                        {
                            if (Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                if (Config.UseFilenameEncryption)
                                    makedirname = Path.GetRandomFileName();
                            }
                            else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                makedirname = CryptCarotDAV.EncryptFilename(short_name);
                            }
                        }

                        var checkpoint = DriveData.ChangeCheckpoint;
                        // make subdirectory
                        int retry = 30;
                        while (--retry > 0)
                        {
                            try
                            {
                                newdir = await Drive.createFolder(makedirname, parent_id, ct);
                                var children = await DriveData.GetChanges(checkpoint, ct);
                                if (children?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                                {
                                    Config.Log.LogOut("createFolder : child found.");
                                    break;
                                }
                                await Task.Delay(2000);
                                continue;
                            }
                            catch (HttpRequestException)
                            {
                                // retry
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            try
                            {
                                var children = await DriveData.GetChanges(checkpoint, ct);
                                if (children?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                                {
                                    Config.Log.LogOut("createFolder : child found.");
                                    if (newdir == null)
                                    {
                                        newdir = children.Where(x => x.name.Contains(makedirname) && x.status == "AVAILABLE").LastOrDefault();
                                    }
                                    break;
                                }
                                await Task.Delay(2000);
                            }
                            catch (HttpRequestException)
                            {
                                // retry
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                        }
                        if (retry == 0)
                        {
                            Config.Log.LogOut("createFolder : (ERROR)child not found.");
                            return -1;
                        }
                        if (Config.UseEncryption && Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            if (!await DriveData.EncryptFilename(uploadfilename: makedirname, enckey: enckey, checkpoint: checkpoint, ct: ct))
                            {
                                Config.Log.LogOut("createFolder : (ERROR)name cryption failed.");
                                return -1;
                            }
                        }
                    }

                    f_cur = await DoFileUpload(Directory.EnumerateFiles(filename), newdir.id, f_all, f_cur, ct);
                    if (f_cur < 0) return -1;

                    f_cur = await DoDirectoryUpload(Directory.EnumerateDirectories(filename), newdir.id, f_all, f_cur, ct);
                    if (f_cur < 0) return -1;
                }
                return f_cur;
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async void button_upload_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Upload Start.");
            toolStripStatusLabel1.Text = "unable to upload.";
            if (!initialized) return;

            string parent_id = listviewitem.Root?.info.id;
            ItemInfo target = listviewitem.Root;
            if (parent_id == null) return;

            toolStripStatusLabel1.Text = "Upload...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            try
            {
                if ((ModifierKeys & Keys.Shift) == Keys.Shift ||
                    (ModifierKeys & Keys.Control) == Keys.Control)
                {
                    folderBrowserDialog1.Description = Resource_text.SelectUploadFolder_str;
                    if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                    {
                        toolStripStatusLabel1.Text = "Canceled.";
                        return;
                    }

                    if (await DoDirectoryUpload(new string[] { folderBrowserDialog1.SelectedPath }, parent_id, 1, 0) < 0) return;
                }
                else
                {
                    openFileDialog1.Title = Resource_text.SelectUploadFiles_str;
                    if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    {
                        toolStripStatusLabel1.Text = "Canceled.";
                        return;
                    }

                    int f_all = openFileDialog1.FileNames.Count();
                    int f_cur = 0;

                    if (await DoFileUpload(openFileDialog1.FileNames, parent_id, f_all, f_cur) < 0) return;
                }
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(2));
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

        public async Task DoTrashItem(IEnumerable<string> trushids)
        {
            if (trushids.Count() == 0) return;
            if (MessageBox.Show(Resource_text.TrashItems_str, "Trash Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            var task = TaskCanceler.CreateTask("TrashItem");
            var ct = task.cts.Token;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items...";
                toolStripProgressBar1.Maximum = trushids.Count();
                toolStripProgressBar1.Step = 1;

                ItemInfo target = listviewitem.Root;

                foreach (var item in trushids)
                {
                    var ret = await Drive.TrashItem(item, ct: ct);
                    toolStripProgressBar1.PerformStep();
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Trash Items done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ReloadItems(target?.info.id, true);
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
                MessageBox.Show("Trash : ERROR\r\n" + ex.Message);
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async void trashItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Trash Start.");
            if (!initialized) return;
            var select = listviewitem.GetItems(listView1.SelectedIndices);

            await DoTrashItem(select.Select(x => x.info.id));
        }

        private async Task ForceReloadItems(string display_id)
        {
            if (string.IsNullOrEmpty(display_id))
                display_id = DriveData.AmazonDriveRootID;
            await TaskCanceler.CancelTask("ReloadItems");
            await TaskCanceler.CancelTask("ForceReloadItems");
            var task = TaskCanceler.CreateTask("ForceReloadItems");
            var ct = task.cts.Token;
            try
            {
                listView1.VirtualListSize = 0;
                treeView1.Nodes.Clear();

                // Load Root
                var changes = await DriveData.GetChanges(ct: ct, 
                    inprogress: (str)=> {
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                        toolStripStatusLabel1.Text = str;
                    },
                    done: (str) => {
                        toolStripStatusLabel1.Text = str;
                    });
                // load tree
                var items = GenerateTreeNode(DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].children.Values, 1);
                treeView1.Nodes.AddRange(items);

                List<string> tree_ids = new List<string>();
                tree_ids.Add(display_id);
                var p = display_id;
                while (p != DriveData.AmazonDriveRootID)
                {
                    p = DriveData.AmazonDriveTree[p].info.parents[0];
                    tree_ids.Add(p);
                }
                tree_ids.Reverse();
                var Nodes = treeView1.Nodes;
                foreach (var t in tree_ids)
                {
                    if (t == DriveData.AmazonDriveRootID) continue;
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
                listviewitem.Root = DriveData.AmazonDriveTree[display_id];
                listView1.VirtualListSize = listviewitem.Count;

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
                TaskCanceler.FinishTask(task);
            }
        }

        private async Task ReloadItems(string display_id, bool getchange = true)
        {
            if (string.IsNullOrEmpty(DriveData.AmazonDriveRootID))
                return;

            if (string.IsNullOrEmpty(display_id))
                display_id = DriveData.AmazonDriveRootID;

            await TaskCanceler.CancelTask("ReloadItems");
            await TaskCanceler.CancelTask("ForceReloadItems");
            var task = TaskCanceler.CreateTask("ReloadItems");
            var ct = task.cts.Token;
            try
            {
                listView1.VirtualListSize = 0;
                treeView1.Nodes.Clear();

                // Load Changed items
                if (getchange)
                {
                    var checkpoint = DriveData.ChangeCheckpoint;
                    await DriveData.GetChanges(
                        checkpoint: checkpoint,
                        ct: ct,
                        inprogress: (str) =>
                        {
                            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                            toolStripStatusLabel1.Text = str;
                        },
                        done: (str) =>
                        {
                            toolStripStatusLabel1.Text = str;
                        });
                }

                // load tree
                var items = GenerateTreeNode(DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].children.Values, 1);
                treeView1.Nodes.AddRange(items);

                List<string> tree_ids = new List<string>();
                tree_ids.Add(display_id);
                var p = display_id;
                while (p != DriveData.AmazonDriveRootID)
                {
                    p = DriveData.AmazonDriveTree[p].info.parents[0];
                    tree_ids.Add(p);
                }
                tree_ids.Reverse();
                var Nodes = treeView1.Nodes;
                foreach (var t in tree_ids)
                {
                    if (t == DriveData.AmazonDriveRootID) continue;
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

                if (listviewitem.IsSearchResult)
                {
                    await DoSearch();
                }
                else
                {
                    //// display listview Root
                    listviewitem.Root = DriveData.AmazonDriveTree[display_id];
                    listView1.VirtualListSize = listviewitem.Count;

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "done.";
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
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async void button_reload_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            string target_id = DriveData.AmazonDriveRootID;
            target_id = listviewitem.Root?.info.id ?? target_id;

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

        public async Task downloadItems(IEnumerable<FileMetadata_Info> target)
        {
            Config.Log.LogOut("Download Start.");
            target = target.SelectMany(x => DriveData.GetAllChildrenfromId(x.id));
            var itembasepath = FormMatch.GetBasePath(target.Select(x => DriveData.GetFullPathfromId(x.id)));
            target = target.Where(x => x.kind == "FILE");
            int f_all = target.Count();
            if (f_all == 0) return;


            int f_cur = 0;
            string savefilename = null;
            string savefilepath = null;

            if (f_all > 1)
            {
                folderBrowserDialog1.Description = "Select Save Folder for Download Items";
                if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
                savefilepath = folderBrowserDialog1.SelectedPath;
            }
            else
            {
                var filename = DriveData.AmazonDriveTree[target.First().id].DisplayName;
                saveFileDialog1.FileName = filename;
                saveFileDialog1.Title = "Select Save Fileneme for Download";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
                savefilename = saveFileDialog1.FileName;
            }

            var task = TaskCanceler.CreateTask("downloads");
            var ct = task.cts.Token;
            try
            {
                foreach (var downitem in target)
                {
                    ct.ThrowIfCancellationRequested();
                    var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;

                    Config.Log.LogOut("Download : " + filename);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = download_str + " " + filename;
                    toolStripProgressBar1.Maximum = 10000;

                    if (savefilepath != null)
                    {
                        var itempath = DriveData.GetFullPathfromId(downitem.id).Substring(itembasepath.Length).Split('/');
                        var dpath = savefilepath;
                        foreach(var p in itempath.Take(itempath.Length - 1))
                        {
                            dpath = Path.Combine(dpath, p);
                            if (!Directory.Exists(dpath)) Directory.CreateDirectory(dpath);
                        }
                        savefilename = Path.Combine(dpath, filename);
                    }

                    var retry = 5;
                    var strerr = "";
                    while (--retry > 0)
                    {
                        try
                        {
                            using (var outfile = File.Open(savefilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                using (var ret = new AmazonDriveBaseStream(Drive, downitem))
                                using (var f = new PositionStream(ret, downitem.OrignalLength ?? 0))
                                {
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        synchronizationContext.Post(
                                            (o) =>
                                            {
                                                if (ct.IsCancellationRequested) return;
                                                var eo = o as PositionChangeEventArgs;
                                                toolStripStatusLabel1.Text = download_str + eo.Log + " " + filename;
                                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                                toolStripProgressBar1.Maximum = 10000;
                                                toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                            }, evnt);
                                    };
                                    await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct);
                                }
                            }
                            Config.Log.LogOut("Download : Done");
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
                    }
                    if (retry == 0)
                    {
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
                TaskCanceler.FinishTask(task);
            }
        }

        private async void downloadItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            await downloadItems(listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.info));
        }

        private async void sendUDPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await TaskCanceler.CancelTask("play");
            await PlayFiles(PlayOneTSFile, "Send UDP");
        }

        private async Task DoSearch()
        {
            string search_str = comboBox_FindStr.Text;
            IEnumerable<ItemInfo> selection = DriveData.AmazonDriveTree.Values;

            if (checkBox_File.Checked && checkBox_Folder.Checked)
                selection = selection.Where(x => x.info.kind != "ASSET");
            else if (checkBox_Folder.Checked)
                selection = selection.Where(x => x.info.kind == "FOLDER");
            else if (checkBox_File.Checked)
                selection = selection.Where(x => x.info.kind != "FOLDER" && x.info.kind != "ASSET");
            else
            {
                // all item selected
            }

            var task = TaskCanceler.CreateTask("Search");
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Create index...";

                await Task.Run(() => {
                    Parallel.ForEach(selection, (item) =>
                    {
                        if(task.cts.Token.IsCancellationRequested) return;
                        var disp = item.DisplayName;
                    }
                    );
                }, task.cts.Token);

                if (checkBox_Regex.Checked)
                {
                    if (checkBox_findCaseSensitive.Checked)
                        selection = selection.Where(x => Regex.IsMatch(x.DisplayName ?? "", search_str));
                    else
                        selection = selection.Where(x => Regex.IsMatch(x.DisplayName ?? "", search_str, RegexOptions.IgnoreCase));
                }
                else
                {
                    if (checkBox_findCaseSensitive.Checked)
                        selection = selection.Where(x => (x.DisplayName?.IndexOf(search_str) >= 0));
                    else
                        selection = selection.Where(x => (
                        System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(
                            x.DisplayName ?? "",
                            search_str,
                            System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreKanaType | System.Globalization.CompareOptions.IgnoreWidth
                            | System.Globalization.CompareOptions.IgnoreNonSpace | System.Globalization.CompareOptions.IgnoreSymbols
                            ) >= 0));
                }

                if (checkBox_sizeOver.Checked)
                    selection = selection.Where(x => (x.info.contentProperties?.size ?? 0) > numericUpDown_sizeOver.Value);
                if (checkBox_sizeUnder.Checked)
                    selection = selection.Where(x => (x.info.contentProperties?.size ?? 0) < numericUpDown_sizeUnder.Value);


                if (radioButton_createTime.Checked)
                {
                    if (checkBox_dateFrom.Checked)
                        selection = selection.Where(x => x.info.createdDate > dateTimePicker_from.Value);
                    if (checkBox_dateTo.Checked)
                        selection = selection.Where(x => x.info.createdDate < dateTimePicker_to.Value);
                }
                if (radioButton_modifiedDate.Checked)
                {
                    if (checkBox_dateFrom.Checked)
                        selection = selection.Where(x => x.info.modifiedDate > dateTimePicker_from.Value);
                    if (checkBox_dateTo.Checked)
                        selection = selection.Where(x => x.info.modifiedDate < dateTimePicker_to.Value);
                }

                toolStripStatusLabel1.Text = "Searching...";

                ItemInfo[] result = null;
                await Task.Run(() =>
                {
                    result = selection.ToArray();
                }, task.cts.Token);

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Found : " + result.Length.ToString();

                listviewitem.SearchResult = result;
                listView1.VirtualListSize = listviewitem.Count;
            }
            catch (TaskCanceledException)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Cancel.";
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async void button_search_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            if(comboBox_FindStr.Items.IndexOf(comboBox_FindStr.Text) < 0)
                comboBox_FindStr.Items.Add(comboBox_FindStr.Text);

            await DoSearch();
        }

        private async void button_mkdir_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("make Folder Start.");
            toolStripStatusLabel1.Text = "unable to mkFolder.";
            if (!initialized) return;

            string parent_id = listviewitem.Root?.info.id;
            if (parent_id == null) return;

            toolStripStatusLabel1.Text = "mkFolder...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            try
            {
                var newdir = await Drive.createFolder(textBox_newName.Text, parent_id);
                await Task.Delay(2000);

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

            var selectItem = listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.info);

            int f_all = selectItem.Count();
            int changecount = 0;
            if (f_all == 0) return;

            if (f_all > 1)
                if (MessageBox.Show(Resource_text.RenameMulti_str, "Rename Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            toolStripStatusLabel1.Text = "Rename...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            var task = TaskCanceler.CreateTask("Rename");
            var ct = task.cts.Token;
            try
            {
                string parent_id = listviewitem.Root?.info.id;

                foreach (var downitem in selectItem)
                {
                    if (DriveData.AmazonDriveTree[downitem.id].IsEncrypted == CryptMethods.Method1_CTR)
                        continue;
                    using (var NewName = new FormInputName())
                    {
                        NewName.NewItemName = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                        if (NewName.ShowDialog() != DialogResult.OK) break;

                        var newfilename = NewName.NewItemName;
                        if (DriveData.AmazonDriveTree[downitem.id].IsEncrypted == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            newfilename = CryptCarotDAV.EncryptFilename(newfilename);
                        }
                        ct.ThrowIfCancellationRequested();
                        changecount++;
                        await Drive.renameItem(downitem.id, newfilename, ct: ct);
                    }
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Rename Items done.";

                if (changecount > 0)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await ReloadItems(parent_id, true);
                }
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
                TaskCanceler.FinishTask(task);
            }
        }

        private async void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (listviewitem.Root != null && (listView1.SelectedIndices.Contains(0) || listView1.SelectedIndices.Contains(1)))
                return;
            ClipboardAmazonDrive data = null;
            var items = listviewitem.GetItems(listView1.SelectedIndices);
            var t = Task.Run(() =>
            {
                data = new ClipboardAmazonDrive(items);
            });
            if((await Task.WhenAny(t, Task.Delay(500))) != t)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Loading drag items...";
                await t;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Done.";
            }
            listView1.DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move);
        }

        private FileMetadata_Info[] GetSelectedItemsFromDataObject(System.Windows.Forms.IDataObject data)
        {
            object ret = null;
            FORMATETC fmt = new FORMATETC { cfFormat = ClipboardAmazonDrive.CF_AMAZON_DRIVE_ITEMS, dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -2, ptd = IntPtr.Zero, tymed = TYMED.TYMED_ISTREAM };
            STGMEDIUM media = new STGMEDIUM();
            (data as System.Runtime.InteropServices.ComTypes.IDataObject).GetData(ref fmt, out media);
            var st = new IStreamWrapper(Marshal.GetTypedObjectForIUnknown(media.unionmember, typeof(IStream)) as IStream);
            st.Position = 0;
            var bf = new BinaryFormatter();
            ret = bf.Deserialize(st);
            ClipboardAmazonDrive.ReleaseStgMedium(ref media);
            return ret as FileMetadata_Info[];
        }

        private void listView1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Point p = listView1.PointToClient(new Point(e.X, e.Y));
                ListViewItem item = listView1.GetItemAt(p.X, p.Y);
                var droptarget = item?.Tag as ItemInfo;
                var current = listviewitem.Root;

                if (!listviewitem.Contains(droptarget?.info.id) || droptarget?.info.kind != "FOLDER")
                {
                    // display root is target
                }
                else
                {
                    current = droptarget;
                }

                if (current != null)
                {
                    if (current.info.kind == "FOLDER")
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                            e.Effect = DragDropEffects.Copy;
                        else
                        {
                            var selectedItems = GetSelectedItemsFromDataObject(e.Data);
                            if ((!selectedItems?.Select(x => x.id).Contains(current.info.id) ?? false) && !current.children.Keys.Intersect(selectedItems?.Select(x => x.id)).Any())
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
                else
                    e.Effect = DragDropEffects.None;
            }
        }

        private async Task DragDrop_AmazonItem(System.Windows.Forms.IDataObject data, string toParent, string logprefix = "")
        {
            Config.Log.LogOut(string.Format("move({0}) Start.", logprefix));
            toolStripStatusLabel1.Text = "Move Item...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            string parent_id = listviewitem.Root?.info.id;
            var task = TaskCanceler.CreateTask(string.Format("move({0})", logprefix));
            try
            {
                var selects = GetSelectedItemsFromDataObject(data);
                int count = 0;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Move Item...";
                toolStripProgressBar1.Maximum = selects.Length;
                toolStripProgressBar1.Step = 1;

                foreach (var aItem in selects)
                {
                    task.cts.Token.ThrowIfCancellationRequested();
                    var fromParent = aItem.parents[0];
                    var childid = aItem.id;

                    toolStripStatusLabel1.Text = string.Format("Move Item... {0}/{1} {2}", ++count, selects.Length, aItem.name);

                    await Drive.moveChild(childid, fromParent, toParent, task.cts.Token);
                    toolStripProgressBar1.PerformStep();
                }

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Move Item done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(2));
                await ReloadItems(parent_id);
                Config.Log.LogOut(string.Format("move({0}) : done.", logprefix));
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    await ReloadItems(parent_id);

                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripProgressBar1.Maximum = 100;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                Config.Log.LogOut(string.Format("move({0}) : Error", logprefix));
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("Move Item : ERROR\r\n" + ex.Message);
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async Task DragDrop_FileDrop(string[] drags, string parent_id, string logprefix = "")
        {
            var task = TaskCanceler.CreateTask(string.Format("upload({0})", logprefix));
            string disp_id = listviewitem.Root?.info.id;
            toolStripStatusLabel1.Text = "Upload...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            try
            {
                string[] dir_drags = null;
                int f_all = 0;
                await Task.Run(() =>
                {
                    dir_drags = drags.Where(x => Directory.Exists(x)).ToArray();
                    drags = drags.Where(x => File.Exists(x)).ToArray();
                    f_all = drags.Length + dir_drags.Select(x => Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories)).SelectMany(i => i).Distinct().Count();
                }, task.cts.Token);

                int f_cur = 0;

                try
                {
                    f_cur = await DoFileUpload(drags, parent_id, f_all, f_cur, task.cts.Token);
                    if (f_cur >= 0)
                        f_cur = await DoDirectoryUpload(dir_drags, parent_id, f_all, f_cur, task.cts.Token);

                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    await ReloadItems(disp_id);
                    Config.Log.LogOut(string.Format("upload({0}) : done.", logprefix));
                }
                catch (OperationCanceledException)
                {
                    if (!Config.IsClosing)
                    {
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        await ReloadItems(disp_id);

                        toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                        toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                        toolStripProgressBar1.Maximum = 100;
                        toolStripStatusLabel1.Text = "Operation Aborted.";
                    }
                }
                catch (Exception ex)
                {
                    Config.Log.LogOut(string.Format("upload({0}) : Error",logprefix));
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                    toolStripStatusLabel1.Text = "Error detected.";
                    MessageBox.Show("Upload Items : ERROR\r\n" + ex.Message);
                }
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }

        private async Task listView_DragDrop_FileDrop(System.Windows.Forms.IDataObject data, string parent_id)
        {
            Config.Log.LogOut("upload(listview) Start.");
            string[] drags = (string[])data.GetData(DataFormats.FileDrop);

            if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            await DragDrop_FileDrop(drags, parent_id, "listview");
        }

        private async void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    Point p = listView1.PointToClient(new Point(e.X, e.Y));
                    ListViewItem item = listView1.GetItemAt(p.X, p.Y);
                    var droptarget = item?.Tag as ItemInfo;
                    var current = listviewitem.Root;

                    if (!listviewitem.Contains(droptarget?.info.id) || droptarget?.info.kind != "FOLDER")
                    {
                        // display root is target
                    }
                    else
                    {
                        current = droptarget;
                    }
                    if (current == null) return;

                    var ParentId = current.info.id;
                    if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS))
                    {
                        await DragDrop_AmazonItem(e.Data, ParentId, "listview");
                    }
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        await listView_DragDrop_FileDrop(e.Data, ParentId);
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
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
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
                        var toParent = (item?.Tag as ItemInfo)?.info.id ?? DriveData.AmazonDriveRootID;
                        foreach (var aItem in GetSelectedItemsFromDataObject(e.Data))
                        {
                            var fromParent = aItem.parents[0];
                            var childid = aItem.id;
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

        private async Task treeView1_DragDrop_AmazonItem(System.Windows.Forms.IDataObject data, string toParent)
        {
            Config.Log.LogOut("move(treeview) Start.");
            toolStripStatusLabel1.Text = "Move Item...";
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;

            try
            {
                string disp_id = listviewitem.Root?.info.id;

                var selects = GetSelectedItemsFromDataObject(data);
                int count = 0;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Move Item...";
                toolStripProgressBar1.Maximum = selects.Length;
                toolStripProgressBar1.Step = 1;

                foreach (var aItem in selects)
                {
                    var fromParent = aItem.parents[0];
                    var childid = aItem.id;
                    toolStripStatusLabel1.Text = string.Format("Move Item... {0}/{1} {2}", ++count, selects.Length, aItem.name);

                    await Drive.moveChild(childid, fromParent, toParent);
                    toolStripProgressBar1.PerformStep();
                }
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Move Item done.";
                toolStripProgressBar1.Maximum = 100;
                toolStripProgressBar1.Step = 10;

                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                await Task.Delay(TimeSpan.FromSeconds(2));
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

        private async Task treeView1_DragDrop_FileDrop(System.Windows.Forms.IDataObject data, string parent_id)
        {
            string disp_id = listviewitem.Root?.info.id;

            Config.Log.LogOut("upload(treeview) Start.");
            string[] drags = (string[])data.GetData(DataFormats.FileDrop);

            if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            await DragDrop_FileDrop(drags, parent_id, "treeview");
        }

        private async void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    Point p = treeView1.PointToClient(new Point(e.X, e.Y));
                    TreeNode item = treeView1.GetNodeAt(p.X, p.Y);

                    if (item != null)
                    {
                        while ((item.Tag as ItemInfo)?.info.kind != "FOLDER")
                        {
                            item = item.Parent;
                            if (item == null) break;
                        }
                    }


                    string ParentId = (item?.Tag as ItemInfo)?.info.id ?? DriveData.AmazonDriveRootID;
                    if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS))
                    {
                        await treeView1_DragDrop_AmazonItem(e.Data, ParentId);
                    }
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        await treeView1_DragDrop_FileDrop(e.Data, ParentId);
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
                if (listviewitem.Root == null)
                {
                    for(int i = 0; i < listviewitem.Items.Length; i++)
                        listView1.SelectedIndices.Add(i);
                }
                else
                {
                    for (int i = 2; i < listviewitem.Items.Length+2; i++)
                        listView1.SelectedIndices.Add(i);
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

        public IEnumerable<FileMetadata_Info> GetSeletctedRemoteFiles()
        {
            if(listView1.SelectedIndices.Count == 0)
                return listviewitem.Items.Select(x => x.info);
            if (listviewitem.Root == null)
                return listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.info);

            if (listView1.SelectedIndices.Contains(0)) listView1.SelectedIndices.Remove(0);
            if (listView1.SelectedIndices.Contains(1)) listView1.SelectedIndices.Remove(1);

            return listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.info);
        }

        private void button_LocalRemoteMatch_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            FormMatch.Instance.SelectedRemoteFiles = GetSeletctedRemoteFiles();
            FormMatch.Instance.Show();
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
            await TaskCanceler.CancelTask("play");
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

        private async Task PlayOneTSFile(FileMetadata_Info downitem, string download_str, CancellationToken ct, dynamic data)
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
                var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;

                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripProgressBar1.Maximum = 10000;
                toolStripStatusLabel1.Text = download_str + " " + filename;

                var internalToken = seekUDP_ct_source.Token;
                var externalToken = ct;
                try
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(internalToken, externalToken))
                    using (var ret = await Drive.downloadFile(downitem, SkipByte, ct: linkedCts.Token))
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
                                    toolStripStatusLabel1.Text = download_str + eo.Log + " " + filename;
                                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                    toolStripProgressBar1.Maximum = 10000;
                                    toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                                }, evnt);
                        };
                        using (var UDP = new UDP_TS_Stream(linkedCts.Token))
                        {
                            label_sendname.Text = filename;
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

        dynamic ffplayer = null;

        private async Task PlayWithFFmpeg()
        {
            await TaskCanceler.CancelTask("play");
            if (!initialized) return;

            var asm = Assembly.LoadFrom("ffmodule.dll");
            var typeInfo = asm.GetType("ffmodule.FFplayer");

            using (dynamic Player = Activator.CreateInstance(typeInfo))
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
                    Player.SetLogger(logwriter);
                    await PlayFiles(new PlayOneFileDelegate(PlayOneFFmpegPlayer), "FFmpeg", data: Player);
                    Player.SetLogger(null);
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

        private Bitmap GetImage(dynamic player)
        {
            try
            {
                var asm = Assembly.Load("ffmodule");
                var typeInfo = asm.GetType("ffmodule.FFplayer");
                
                Bitmap ret = null;
                FileMetadata_Info downitem = player.Tag as FileMetadata_Info;
                ImageCodecInfo[] decoders = ImageCodecInfo.GetImageDecoders();
                string filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                var target = DriveData.AmazonDriveTree[downitem.parents[0]].children.Where(x => x.Value.DisplayName.StartsWith(Path.GetFileNameWithoutExtension(filename)));
                foreach (var t in target)
                {
                    var ext = Path.GetExtension(t.Value.info.name).ToLower();
                    foreach (var ici in decoders)
                    {
                        bool found = false;
                        var decext = ici.FilenameExtension.Split(';').Select(x => Path.GetExtension(x).ToLower()).ToArray();
                        if (decext.Contains(ext))
                        {
                            CancellationToken ct = player.ct;
                            Drive.downloadFile(t.Value.info, ct: ct).ContinueWith(task =>
                            {
                                using (var st = task.Result)
                                {
                                    var img = Image.FromStream(st);
                                    ret = new Bitmap(img);
                                    found = true;
                                }
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

        private async Task PlayOneFFmpegPlayer(FileMetadata_Info downitem, string download_str, CancellationToken ct, dynamic data)
        {
            string filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
            toolStripProgressBar1.Maximum = 10000;
            toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
            toolStripStatusLabel1.Text = download_str + " " + filename;
            label_FFplay_sendname.Text = filename;
            var Player = data;
            timer3.Enabled = true;
            Player.StartSkip = FFplayStartDelay;
            Player.StopDuration = FFplayDuration;
            await Task.Run(() =>
            {
                using (var driveStream = new AmazonDriveSeekableStream(Drive, downitem))
                using (var PosStream = new PositionStream(driveStream))
                {
                    PosStream.PosChangeEvent += (src, evnt) =>
                    {
                        synchronizationContext.Post(
                            (o) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                var eo = o as PositionChangeEventArgs;
                                toolStripStatusLabel1.Text = download_str + eo.Log + " " + filename;
                                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                                toolStripProgressBar1.Maximum = 10000;
                                toolStripProgressBar1.Value = (int)((double)eo.Position / eo.Length * 10000);
                            }, evnt);
                    };
                    Player.Tag = downitem;
                    if (Player.Play(PosStream, filename, ct) != 0)
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

        private delegate Task PlayOneFileDelegate(FileMetadata_Info downitem, string download_str, CancellationToken ct, dynamic data);

        private async Task PlayFiles(PlayOneFileDelegate func, string LogPrefix, CancellationToken ct = default(CancellationToken), dynamic data = null)
        {
            Config.Log.LogOut(LogPrefix + " media files Start.");
            toolStripStatusLabel1.Text = "unable to download.";
            if (!initialized) return;
            var select = listView1.SelectedIndices;
            if (select.Count == 0) return;

            var selectItem = listviewitem.GetItems(select).Select(x => x.info).Where(x => x.kind != "FOLDER").ToArray();

            int f_all = selectItem.Count();
            if (f_all == 0) return;

            int f_cur = 0;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("play");
                ct = task.cts.Token;
            }
            try
            {
                nextUDPcount = 0;
                foreach (var downitem in selectItem)
                {
                    ct.ThrowIfCancellationRequested();
                    var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                    Config.Log.LogOut(LogPrefix + " download : " + filename);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    if (downitem.contentProperties.size > ConfigAPI.FilenameChangeTrickSize && !Regex.IsMatch(downitem.name, "^[\x20-\x7e]*$"))
                    {
                        Config.Log.LogOut(LogPrefix + " download : <BIG FILE> temporary filename change");
                        Interlocked.Increment(ref Config.AmazonDriveTempCount);
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
                            Interlocked.Decrement(ref Config.AmazonDriveTempCount);
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
                TaskCanceler.FinishTask(task);
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

        private async void button_Playbreak_Click(object sender, EventArgs e)
        {
            await TaskCanceler.CancelTask("play");
        }

        private void buttonFFmpegmoduleConfig_Click(object sender, EventArgs e)
        {
            var form = new FormFFmoduleConfig();
            form.ShowDialog();
        }

        private void button_breakall_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView_TaskList.Items)
                (item.Tag as TaskCanselToken).cts.Cancel();
        }

        private void amazonDriveHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var logform = new FormDriveLog();
            logform.ChangeLog = DriveData.AmazonDriveHistory;
            logform.ShowDialog();
        }

        private void checkBox_LockPassword_CheckedChanged(object sender, EventArgs e)
        {
            Config.LockPassword = checkBox_LockPassword.Checked;
            textBox_Password.Enabled = !checkBox_LockPassword.Checked;
            textBox_Password.PasswordChar = (Config.LockPassword) ? '*' : '\0';
        }

        private void textBox_Password_TextChanged(object sender, EventArgs e)
        {
            Config.DrivePassword = textBox_Password.Text;
        }

        private void checkBox_crypt_CheckedChanged(object sender, EventArgs e)
        {
            Config.UseEncryption = checkBox_crypt.Checked;
        }

        private void checkBox_cryptfilename_CheckedChanged(object sender, EventArgs e)
        {
            Config.UseFilenameEncryption = checkBox_cryptfilename.Checked;
            if (Config.UseFilenameEncryption)
                checkBox_crypt.Checked = true;
        }


        private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            defaultToolStripMenuItem.Checked = true;
            englishToolStripMenuItem.Checked = false;
            japaneseToolStripMenuItem.Checked = false;
            Config.Language = "";
        }

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            englishToolStripMenuItem.Checked = true;
            defaultToolStripMenuItem.Checked = false;
            japaneseToolStripMenuItem.Checked = false;
            Config.Language = "en";
        }

        private void japaneseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            japaneseToolStripMenuItem.Checked = true;
            defaultToolStripMenuItem.Checked = false;
            englishToolStripMenuItem.Checked = false;
            Config.Language = "ja";
        }

        private void button_TestDownload_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            var test = new FormTestDownload();
            test.SelectedRemoteFiles = GetSeletctedRemoteFiles();
            test.Show();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private async Task MakeTempDownloadLinks(IEnumerable<FileMetadata_Info> selectItem, CancellationToken ct = default(CancellationToken))
        {

            Config.Log.LogOut("MakeTempDownloadLinks : start");
            int f_all = selectItem.Count();
            if (f_all == 0) return;

            var templinks = new List<FileMetadata_Info>();
            int f_cur = 0;
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("templink");
                ct = task.cts.Token;
            }
            try
            {
                foreach (var item in selectItem)
                {
                    ct.ThrowIfCancellationRequested();
                    Config.Log.LogOut("MakeTempLink : " + item.name);
                    var download_str = (f_all > 1) ? string.Format("TempLink({0}/{1})...", ++f_cur, f_all) : "TempLink...";

                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    int retry = 6;
                    while (--retry > 0)
                    {
                        try
                        {
                            var newitem = await Drive.GetFileMetadata(item.id, ct: ct, templink: true);
                            templinks.Add(newitem);
                            break;
                        }
                        catch (HttpRequestException)
                        {
                            //retry
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Maximum = f_all;
                    toolStripProgressBar1.Value = f_cur;
                    toolStripStatusLabel1.Text = download_str + " " + item.name;
                }
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "MakeTempLink done.";
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
                Config.Log.LogOut("MakeTempDownloadLinks : error.");
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripProgressBar1.Value = toolStripProgressBar1.Minimum;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("MakeTempDownloadLinks : ERROR\r\n" + ex.Message);
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
            if (templinks.Count > 1)
            {
                var logform = new FormTemplink();
                logform.TempLinks = templinks;
                logform.ShowDialog();
            }
            else if(templinks.Count == 1)
            {
                Clipboard.SetText(templinks[0].tempLink);
            }
        }

        private async void makeTemporaryLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            var select = listView1.SelectedIndices;
            if (select.Count == 0) return;

            var selectItem = listviewitem.GetItems(select).Select(x => x.info).Where(x => x.kind != "FOLDER").ToArray();
            await MakeTempDownloadLinks(selectItem);
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            e.Item = listviewitem[e.ItemIndex];
        }

        private void sortBykindOfItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sortBykindOfItemToolStripMenuItem.Checked = !sortBykindOfItemToolStripMenuItem.Checked;
            listviewitem.SortKind = sortBykindOfItemToolStripMenuItem.Checked;
            listView1.Refresh();
        }

        private void radioButton_crypt_1_CTR_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_crypt_1_CTR.Checked) Config.CryptMethod = CryptMethods.Method1_CTR;
        }

        private void radioButton_crypt_2_CBC_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_crypt_2_CBC.Checked)
            {
                Config.CryptMethod = CryptMethods.Method2_CBC_CarotDAV;
                checkBox_cryptfilename.Checked = true;
            }
        }

        private async void checkBox_decodeView_CheckedChanged(object sender, EventArgs e)
        {
            Config.AutoDecode = checkBox_decodeView.Checked;
            await ReloadItems(listviewitem.Root?.info.id, false);
        }

        private async void comboBox_CarotDAV_Escape_SelectedIndexChanged(object sender, EventArgs e)
        {
            Config.CarotDAV_CryptNameHeader = comboBox_CarotDAV_Escape.Text;
            try
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Apply changes...";
                await DriveData.ChangeCryption2();
                await ReloadItems(listviewitem.Root?.info.id, false);
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Done.";
            }
            catch (OperationCanceledException)
            {
                if (!Config.IsClosing)
                {
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripStatusLabel1.Text = "Operation Aborted.";
                }
            }
            catch (Exception ex)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Error detected.";
                MessageBox.Show("comboBox_CarotDAV_Escape_SelectedIndexChanged : ERROR\r\n" + ex.Message);
                Config.Log.LogOut(ex.ToString());
            }
        }

        private async void textBox_Password_Leave(object sender, EventArgs e)
        {
            await DriveData.ChangeCryption1();
            await ReloadItems(listviewitem.Root?.info.id, false);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listviewitem.Root != null && (listView1.SelectedIndices.Contains(0) || listView1.SelectedIndices.Contains(1)))
                return;
            try
            {
                Clipboard.SetDataObject(new ClipboardAmazonDrive(listviewitem.GetItems(listView1.SelectedIndices, IncludeSpetial: false)));
            }
            catch (ArgumentException)
            {
                // nothing
            }
        }

        private async void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = listviewitem.GetItems(listView1.SelectedIndices).ToArray();
            var current = listviewitem.Root;
            if(selection.Count() == 1)
            {
                var select = selection.First();
                if (select.info?.kind == "FOLDER")
                    current = select;
            }
            if (current == null) return;

            var ParentId = current.info.id;
            if (Clipboard.ContainsData(DataFormats.FileDrop)) {
                Config.Log.LogOut("upload(clipboard) Start.");
                string[] drags = (string[])Clipboard.GetData(DataFormats.FileDrop);

                if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                    if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

                await DragDrop_FileDrop(drags, ParentId, "clipboard");
                return;
            }
            System.Windows.Forms.IDataObject data = Clipboard.GetDataObject();
            var formats = data.GetFormats();
            if (formats.Contains(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS)) {
                await DragDrop_AmazonItem(data, ParentId, "clipboard");
            }
        }

    }

    static class Extensions
    {
        public static IOrderedEnumerable<ItemInfo> SortByKind(this IEnumerable<ItemInfo> x, bool SortKind)
        {
            return x.OrderBy(y => (SortKind) ? (y.info.kind != "FOLDER") : true).ThenBy(y => (SortKind)? y.IsEncrypted: CryptMethods.Unknown);
        }
    }
}

