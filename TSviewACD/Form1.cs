using System;
using System.Collections;
using System.Collections.Concurrent;
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
        int oldDpi;
        int currentDpi;

        public Form1()
        {
            InitializeComponent();
            float dx, dy;
            using (var g = CreateGraphics())
            {
                dx = g.DpiX;
                dy = g.DpiY;

            }
            currentDpi = (int)dx;

            toolStripMenuItem_Logout.Enabled = false;
            synchronizationContext = SynchronizationContext.Current;
            treeView1.Sorted = true;
            InitializeListView();

            HandleDpiChanged();
            Config.Log.LogOut("Application Start.");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Login();
        }


        const int WM_DPICHANGED = 0x02E0;

        private bool needAdjust = false;
        private bool isMoving = false;

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            isMoving = true;
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            isMoving = false;
            if (needAdjust)
            {
                needAdjust = false;
                HandleDpiChanged();
            }
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if(needAdjust && IsLocationGood())
            {
                needAdjust = false;
                HandleDpiChanged();
            }
        }

        private bool IsLocationGood()
        {
            if (oldDpi == 0) return false;

            float scaleFactor = (float)currentDpi / oldDpi;
            Config.Log.LogOut(string.Format("c:{0} o:{1} scale{2}", currentDpi, oldDpi, scaleFactor));

            int widthDiff = (int)(ClientSize.Width * scaleFactor) - ClientSize.Width;
            int heightDiff = (int)(ClientSize.Height * scaleFactor) - ClientSize.Height;

            var rect = new W32.RECT() {
                left = Bounds.Left,
                top = Bounds.Top,
                right = Bounds.Right + widthDiff,
                bottom = Bounds.Bottom + heightDiff
            };

            var handleMonitor = W32.MonitorFromRect(ref rect, W32.MONITOR_DEFAULTTONULL);

            if(handleMonitor != IntPtr.Zero)
            {
                if (W32.GetDpiForMonitor(handleMonitor, W32.Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint dpiY) == 0)
                {
                    if (dpiX == currentDpi)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DPICHANGED:
                    oldDpi = currentDpi;
                    currentDpi = m.WParam.ToInt32() & 0xFFFF;

                    if (oldDpi != currentDpi)
                    {
                        if (isMoving)
                        {
                            needAdjust = true;
                        }
                        else
                        {
                            HandleDpiChanged();
                        }
                    }
                    else
                    {
                        needAdjust = false;
                    }
                    break;
            }

            base.WndProc(ref m);
        }


        const int designTimeDpi = 96;

        private void HandleDpiChanged()
        {
            if (oldDpi != 0)
            {
                float scaleFactor = (float)currentDpi / oldDpi;
                Config.Log.LogOut(string.Format("Dpi c:{0} o:{1} scale{2}", currentDpi, oldDpi, scaleFactor));

                //the default scaling method of the framework
                Scale(new SizeF(scaleFactor, scaleFactor));

                //fonts are not scaled automatically so we need to handle this manually
                ScaleFonts(scaleFactor);

                //perform any other scaling different than font or size (e.g. ItemHeight)
                PerformSpecialScaling(scaleFactor);
            }
            else
            {
                //the special scaling also needs to be done initially
                PerformSpecialScaling((float)currentDpi / designTimeDpi);
            }
        }

        protected virtual void PerformSpecialScaling(float scaleFactor)
        {
            foreach(ColumnHeader c in listView1.Columns)
            {
                c.Width = (int)(c.Width * scaleFactor);
            }
        }

        protected virtual void ScaleFonts(float scaleFactor)
        {
            Font = new Font(Font.FontFamily,
                   Font.Size * scaleFactor,
                   Font.Style);
            //ScaleFontForControl(this, scaleFactor);
        }

        private static void ScaleFontForControl(Control control, float factor)
        {
            control.Font = new Font(control.Font.FontFamily,
                   control.Font.Size * factor,
                   control.Font.Style);

            foreach (Control child in control.Controls)
            {
                ScaleFontForControl(child, factor);
            }
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
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => (Config.AutoDecode) ? y.DisplayName : y.Info.name);
                            case ListColums.Size:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.Info.contentProperties?.size ?? 0);
                            case ListColums.modifiedDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.Info.modifiedDate);
                            case ListColums.createdDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.Info.createdDate);
                            case ListColums.path:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => DriveData.GetFullPathfromItem(y));
                            case ListColums.MD5:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => y.Info.contentProperties?.md5 ?? "");
                            default:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenBy(y => (Config.AutoDecode) ? y.DisplayName : y.Info.name);
                        }
                    }
                    else
                    {
                        switch (_SortColum)
                        {
                            case ListColums.Name:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => (Config.AutoDecode) ? y.DisplayName : y.Info.name);
                            case ListColums.Size:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.Info.contentProperties?.size ?? 0);
                            case ListColums.modifiedDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.Info.modifiedDate);
                            case ListColums.createdDate:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.Info.createdDate);
                            case ListColums.path:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => DriveData.GetFullPathfromItem(y));
                            case ListColums.MD5:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => y.Info.contentProperties?.md5 ?? "");
                            default:
                                return (IEnumerable<ItemInfo> x) => x.SortByKind(_SortKind).ThenByDescending(y => (Config.AutoDecode) ? y.DisplayName : y.Info.name);
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
                foreach (int i in indices)
                {
                    if (Root == null)
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
                    if (value == null)
                    {
                        _Root = null;
                        _Parent = null;
                        _Items = new ItemInfo[0];
                    }
                    else
                    {
                        _Root = value;
                        if (_Root.Info.id == DriveData.AmazonDriveRootID)
                        {
                            _Parent = _Root;
                        }
                        else
                        {
                            _Parent = DriveData.AmazonDriveTree[_Root.Info.parents[0]];
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
                            (Config.AutoDecode)? x.DisplayName: x.Info.name,
                            x.Info.contentProperties?.size?.ToString("#,0"),
                            x.Info.modifiedDate.ToString(),
                            x.Info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(x),
                            x.Info.id,
                            x.Info.contentProperties?.md5,
                    }, (x.Info.kind == "FOLDER") ? 0 : 2);
                item.Name = (Config.AutoDecode) ? x.DisplayName : x.Info.name;
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
                    case CryptMethods.Method3_Rclone:
                        item.ForeColor = Color.Orchid;
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
                    if (_Root == null)
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
                            root.Info.modifiedDate.ToString(),
                            root.Info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(root),
                            root.Info.id,
                            "",
                            }, 0);
                        rootitem.Tag = root;
                        rootitem.Name = (root.Info.id == DriveData.AmazonDriveRootID) ? "/" : ".";
                        rootitem.ToolTipText = Resource_text.CurrentFolder_str;
                        return rootitem;
                    }
                    if (index == 1)
                    {
                        var up = Parent;
                        var upitem = new ListViewItem(
                            new string[] {
                            "..",
                            "",
                            up.Info.modifiedDate.ToString(),
                            up.Info.createdDate.ToString(),
                            DriveData.GetFullPathfromItem(up),
                            up.Info.id,
                            "",
                            }, 0);
                        upitem.Tag = up;
                        upitem.Name = (up.Info.id == DriveData.AmazonDriveRootID) ? "/" : "..";
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
                return (Root?.Info.id == id) || (Parent?.Info.id == id) || (Items.Select(x => x.Info.id).Contains(id));
            }
            public bool Contains(IEnumerable<string> id)
            {
                return id.Select(x => Contains(x)).Any();
            }
        }

        AmazonListViewItem listviewitem = new AmazonListViewItem();

        private void ChageDisplay(ItemInfo Root)
        {
            listviewitem.Root = Root;
            listView1.VirtualListSize = listviewitem.Count;
            DisplayItems(Root?.Info.id);
        }

        private void Login()
        {
            Config.Log.LogOut("Login Start.");
            var job = JobControler.CreateNewJob();
            job.DisplayName = "Login";
            var ct = job.ct;
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                job.ProgressStr = "Login...";
                Drive.Login(ct).ContinueWith((task) =>
                {
                    if (!task.Result)
                    {
                        initialized = false;
                        return;
                    }
                    Drive.GetEndpoint(ct).ContinueWith((task2) =>
                    {
                        if (task.Result)
                        {
                            initialized = true;
                            return;
                        }
                    }, ct).Wait(ct);
                }, ct).Wait(ct);
                if (initialized)
                {
                    job.ProgressStr = "done.";
                    job.Progress = 1;
                    synchronizationContext.Post((o) =>
                    {
                        loginToolStripMenuItem.Enabled = false;
                        toolStripMenuItem_Logout.Enabled = true;
                    }, null);
                }
                else
                {
                    job.Error("Login failed.");
                }
            });
            InitView(job);
        }

        private async Task Logout()
        {
            Config.Log.LogOut("Logout Start.");
            if (JobControler.CancelAll())
            {
                while(!JobControler.IsEmpty)
                    await Task.Delay(100);
            }
            initialized = false;
            DriveData.RemoveCache();
            DriveData.Drive = new AmazonDrive();
            Drive = DriveData.Drive;
            Config.refresh_token = "";
            Config.contentUrl = "";
            Config.metadataUrl = "";
            Config.URL_time = default(DateTime);
            Config.Save();
            treeView1.Nodes.Clear();
            listviewitem.Clear();
            listView1.VirtualListSize = 0;
            loginToolStripMenuItem.Enabled = true;
            toolStripMenuItem_Logout.Enabled = false;
        }


        private TreeNode[] GenerateTreeNode(IEnumerable<ItemInfo> children, int count = 0)
        {
            var ret = new List<TreeNode>();
            Parallel.ForEach(children, () => new List<TreeNode>(), (x, state, local) =>
             {
                 int img = (x.Info.kind == "FOLDER") ? 0 : 2;
                 var node = new TreeNode((Config.AutoDecode) ? x.DisplayName : x.Info.name, img, img);
                 node.Name = (Config.AutoDecode) ? x.DisplayName : x.Info.name;
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
                     case CryptMethods.Method3_Rclone:
                         node.ForeColor = Color.Orchid;
                         break;
                 }
                 if (x.Info.kind == "FOLDER" && count > 0 && x.children.Count > 0)
                 {
                     node.Nodes.AddRange(GenerateTreeNode(x.children.Values, count - 1));
                 }
                 ItemInfo value;
                 if (DriveData.AmazonDriveTree.TryGetValue(x.Info.id, out value))
                 {
                     value.tree = node;
                 }
                 else
                 {
                     DriveData.AmazonDriveTree[x.Info.id] = new ItemInfo(null);
                     DriveData.AmazonDriveTree[x.Info.id].tree = node;
                 }
                 local.Add(node);
                 return local;
             },
            (result) =>
            {
                lock (ret)
                    ret.AddRange(result);
            }
            );
            return ret.ToArray();
        }

        private void InitView(JobControler.Job prevJob)
        {
            // Load Drive Tree
            var job = AmazonDriveControl.InitAlltree(prevJob);

            // Refresh Drive Tree
            var wait = JobControler.CreateNewJob(JobControler.JobClass.WaitReload, depends: job);
            JobControler.Run(wait, (j) =>
            {
                synchronizationContext.Send((o) =>
                {
                    ChageDisplay(DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID]);
                }, null);
                ReloadItems(DriveData.AmazonDriveRootID, AmazonDriveControl.ReloadType.Cache);
            });
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
            listView1.Columns.Add("modifiedDate", 130);
            listView1.Columns.Add("createdDate", 130);
            listView1.Columns.Add("path", 100);
            listView1.Columns.Add("id");
            listView1.Columns.Add("MD5");

            listView1.Columns[1].TextAlign = HorizontalAlignment.Right;
        }

        private void LoadTreeItem(TreeNode node)
        {
            var nodedata = node.Tag as ItemInfo;
            if (nodedata.Info.kind != "FOLDER") return;

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
                    var p = DriveData.AmazonDriveTree[target_id].Info.parents[0];
                    while (DriveData.AmazonDriveTree[p].tree == null)
                    {
                        tree_ids.Add(p);
                        p = DriveData.AmazonDriveTree[p].Info.parents[0];
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
            ChageDisplay(DriveData.AmazonDriveTree[target_id]);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Config.IsMasterPasswordCorrect)
            {
                using(var f = new FormMasterPass())
                    f.ShowDialog();
                if (!Config.IsMasterPasswordCorrect)
                    Close();
            }
            var tempLocation = Location;
            var tempSize = Size;
            if (Config.Main_Location != null)
                Location = Config.Main_Location.Value;
            if (Config.Main_Size != null)
                Size = Config.Main_Size.Value;
            if (!Screen.GetWorkingArea(this).IntersectsWith(Bounds))
            {
                Location = tempLocation;
                Size = tempSize;
            }
            LoadImage();
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
            textBox_Password2.Text = Config.DrivePassword2;
            checkBox_crypt.Checked = Config.UseEncryption;
            checkBox_cryptfilename.Checked = Config.UseFilenameEncryption;
            checkBox_LockPassword.Checked = Config.LockPassword;
            checkBox_LockPassword2.Checked = Config.LockPassword2;
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
            bool crypton = Config.UseEncryption;
            switch (Config.CryptMethod)
            {
                case CryptMethods.Method1_CTR:
                    radioButton_crypt_1_CTR.Checked = true;
                    break;
                case CryptMethods.Method2_CBC_CarotDAV:
                    radioButton_crypt_2_CBC.Checked = true;
                    break;
                case CryptMethods.Method3_Rclone:
                    radioButton_crypt_3_Rclone.Checked = true;
                    break;
            }
            Config.UseEncryption = crypton;
            checkBox_crypt.Checked = Config.UseEncryption;
            checkBox_decodeView.Checked = Config.AutoDecode;
            comboBox_CarotDAV_Escape.Items.AddRange(Config.CarotDAV_crypt_names);
            comboBox_CarotDAV_Escape.Text = Config.CarotDAV_CryptNameHeader;
            numericUpDown_ParallelDownload.Value = Config.ParallelDownload;
            numericUpDown_ParallelUpload.Value = Config.ParallelUpload;

            AmazonDriveControl.DoReload = ReloadItems;
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

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (JobControler.CancelAll() || Config.AmazonDriveTempCount > 0)
            {
                e.Cancel = true;
                if (!TSviewACD.FormClosing.Instance.Visible)
                {
                    TSviewACD.FormClosing.Instance.Show();
                    Application.DoEvents();
                }
                await Task.Delay(500);
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

        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Login();
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
                    ChageDisplay(DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID]);
                    return;
                }

                var selectdata = e.Node.Tag as ItemInfo;
                if (selectdata == null) return;

                if (selectdata.Info.kind == "FOLDER")
                {
                    ChageDisplay(selectdata);
                }
                else
                {
                    ChageDisplay(DriveData.AmazonDriveTree[selectdata.Info.parents[0]]);
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

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            var selectdata = listviewitem.GetItems(listView1.SelectedIndices).FirstOrDefault();

            if (selectdata == null) return;
            if (selectdata.Info.kind == "FOLDER")
            {
                ChageDisplay(selectdata);
                listView1.SelectedIndices.Clear();
                listView1.Refresh();
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_SendUDP")
            {
                PlayFiles(PlayOneTSFile, "Send UDP");
            }
            else if (tabControl1.SelectedTab.Name == "tabPage_FFmpeg")
            {
                PlayWithFFmpeg();
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

        private void button_upload_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Upload Start.");
            if (!initialized) return;

            string parent_id = listviewitem.Root?.Info.id;
            ItemInfo target = listviewitem.Root;
            if (parent_id == null) return;

            JobControler.Job[] upjob = null;
            if ((ModifierKeys & Keys.Shift) == Keys.Shift ||
                (ModifierKeys & Keys.Control) == Keys.Control)
            {
                folderBrowserDialog1.Description = Resource_text.SelectUploadFolder_str;
                if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var job = JobControler.CreateNewJob();
                job.DisplayName = "Upload Follder";
                job.ProgressStr = "Searching...";
                JobControler.Run(job, (j) =>
                {
                    job.Progress = -1;
                    upjob = AmazonDriveControl.DoDirectoryUpload(new string[] { folderBrowserDialog1.SelectedPath }, parent_id, WeekDepend: true, parentJob: job);
                    ReloadAfterJob(upjob, target?.Info.id);
                    job.Progress = 1;
                    job.ProgressStr = "done.";
                });
            }
            else
            {
                openFileDialog1.Title = Resource_text.SelectUploadFiles_str;
                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                upjob = AmazonDriveControl.DoFileUpload(openFileDialog1.FileNames, parent_id);
                ReloadAfterJob(upjob, target?.Info.id);
            }
        }

        public void ReloadAfterJob(JobControler.Job[] mainjobs)
        {
            ReloadAfterJob(mainjobs, listviewitem.Root?.Info.id);
        }

        private void ReloadAfterJob(JobControler.Job[] mainjobs, string reload_id)
        {
            var wait1 = JobControler.CreateNewJob(type: JobControler.JobClass.WaitChanges, depends: mainjobs);
            wait1.DisplayName = "Wait changes";
            wait1.DoAlways = true;
            var ct1 = wait1.ct;
            var checkpoint = DriveData.ChangeCheckpoint;
            JobControler.Run(wait1, (j) =>
            {
                Task.Delay(TimeSpan.FromSeconds(1), ct1).Wait(ct1);
                DriveData.GetChanges(checkpoint, ct1).Wait(ct1);
                ReloadItems(reload_id);
            });
        }

        public void DoTrashItem(IEnumerable<string> trushids)
        {
            var count = trushids.Count();
            if (count == 0) return;
            if (MessageBox.Show(Resource_text.TrashItems_str, "Trash Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            ItemInfo target = listviewitem.Root;
            var joblist = new List<JobControler.Job>();
            foreach (var item in trushids)
            {
                var job = JobControler.CreateNewJob(type: JobControler.JobClass.Trash);
                job.DisplayName = DriveData.GetFullPathfromId(item);
                job.ProgressStr = "wait for TrashItem.";
                joblist.Add(job);
                var ct = job.ct;
                JobControler.Run(job, (j) =>
                {
                    job.ProgressStr = "Trash...";
                    job.Progress = -1;
                    Drive.TrashItem(item, ct: ct).Wait(ct);
                    job.ProgressStr = "Trash done.";
                    job.Progress = 1;
                });
            }

            ReloadAfterJob(joblist.ToArray(), target?.Info.id);
        }

        private void trashItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("Trash Start.");
            if (!initialized) return;
            var select = listviewitem.GetItems(listView1.SelectedIndices);

            DoTrashItem(select.Select(x => x.Info.id));
        }

        private ConcurrentDictionary<string, int> ReloadRequests = new ConcurrentDictionary<string, int>();
        private JobControler.Job ReloadJob;
        private Task ReloadWait;
        private DateTime ReloadTime = DateTime.Now;

        private void DisplayItems(string target_id)
        {
            int count = 0;
            if (ReloadRequests.TryGetValue(target_id, out count))
            {
                ReloadItems(target_id, AmazonDriveControl.ReloadType.GetChanges);
            }
        }
        private void ReloadItems(string reload_target_id, AmazonDriveControl.ReloadType type = AmazonDriveControl.ReloadType.Cache)
        {
            if(reload_target_id == null && DriveData.AmazonDriveRootID != null)
                ReloadRequests.AddOrUpdate(DriveData.AmazonDriveRootID, 1, (key, val) => { return val + 1; });
            else if(reload_target_id != null)
                ReloadRequests.AddOrUpdate(reload_target_id, 1, (key, val) => { return val + 1; });
            var disp_id = listviewitem.Root?.Info.id;
            if(disp_id == null)
            {
                //search result
            }
            else
            {
                int count = 0;
                if(ReloadRequests.TryRemove(disp_id, out count))
                {
                    if (ReloadJob == null)
                    {
                        ReloadJob = JobControler.CreateNewJob(JobControler.JobClass.Reload);
                        ReloadJob.DisplayName = "Reload "+DriveData.GetFullPathfromId(disp_id);
                        var ct = ReloadJob.ct;
                        JobControler.Run(ReloadJob, (j) =>
                        {
                            ReloadJob.Progress = -1;
                            ReloadJob.ProgressStr = "Loading...";

                            var ty = (disp_id == reload_target_id)? type: AmazonDriveControl.ReloadType.Cache;

                            synchronizationContext.Post((o) =>
                            {
                                listView1.VirtualListSize = 0;
                                treeView1.Nodes.Clear();
                                TSviewACD.FormClosing.Instance.Show();
                            }, null);

                            if (ty != AmazonDriveControl.ReloadType.Cache)
                            {
                                var checkpoint = (ty == AmazonDriveControl.ReloadType.GetChanges) ? DriveData.ChangeCheckpoint : null;
                                ReloadRequests.Clear();
                                DriveData.GetChanges(
                                    checkpoint: checkpoint,
                                    ct: ct,
                                    inprogress: (str) =>
                                    {
                                        ReloadJob.ProgressStr = str;
                                    },
                                    done: (str) =>
                                    {
                                        ReloadJob.ProgressStr = str;
                                    }).Wait(ct);
                            }

                            synchronizationContext.Send((o) =>
                            {
                                JobControler.IsReloading = true;
                                try
                                {
                                    // load tree
                                    var items = GenerateTreeNode(DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].children.Values, 1);
                                    treeView1.Nodes.AddRange(items);

                                    List<string> tree_ids = new List<string>();
                                    tree_ids.Add(disp_id);
                                    var p = disp_id;
                                    while (p != DriveData.AmazonDriveRootID)
                                    {
                                        p = DriveData.AmazonDriveTree[p].Info.parents[0];
                                        tree_ids.Add(p);
                                    }
                                    tree_ids.Reverse();
                                    var Nodes = treeView1.Nodes;
                                    foreach (var t in tree_ids)
                                    {
                                        if (t == DriveData.AmazonDriveRootID) continue;
                                        var i = Nodes.OfType<TreeNode>().Where(x => (x.Tag as ItemInfo).Info.id == t);
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
                                        DoSearch();
                                    }
                                    else
                                    {
                                        //// display listview Root
                                        ChageDisplay(DriveData.AmazonDriveTree[disp_id]);
                                    }
                                    ReloadJob.ProgressStr = "done.";
                                    ReloadJob.Progress = 1;
                                }
                                finally
                                {
                                    JobControler.IsReloading = false;
                                    Task.Delay(TimeSpan.FromSeconds(5), ReloadJob.ct).ContinueWith((task) =>
                                    {
                                        ReloadJob = null;
                                    }, ReloadJob.ct);
                                    TSviewACD.FormClosing.Instance.Close();
                                }
                            }, null);
                        });
                    }
                    else //ReloadJob != null
                    {
                        if(ReloadWait == null)
                        {
                            ReloadWait = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith((task) =>
                            {
                                try
                                {
                                    ReloadJob?.Wait();
                                }
                                catch { }
                                ReloadItems(reload_target_id, AmazonDriveControl.ReloadType.GetChanges);
                            }).ContinueWith(task =>
                            {
                                ReloadWait = null;
                            });
                        }
                    }
                }
            }
        }


        private void button_reload_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            string target_id = DriveData.AmazonDriveRootID;
            target_id = listviewitem.Root?.Info.id ?? target_id;

            if ((ModifierKeys & Keys.Shift) == Keys.Shift ||
                (ModifierKeys & Keys.Control) == Keys.Control)
            {
                ReloadItems(target_id, AmazonDriveControl.ReloadType.All);
            }
            else
            {
                ReloadItems(target_id, AmazonDriveControl.ReloadType.GetChanges);
            }
        }

        public JobControler.Job[] downloadItems(IEnumerable<FileMetadata_Info> target)
        {
            Config.Log.LogOut("Download Start.");
            int f_all = target.Count();
            if (f_all == 0) return null;

            if (f_all > 1 || target.First().kind == "FOLDER")
            {
                folderBrowserDialog1.Description = "Select Save Folder for Download Items";
                if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return null;
                return AmazonDriveControl.downloadItems(target, folderBrowserDialog1.SelectedPath);
            }
            else
            {
                var filename = DriveData.AmazonDriveTree[target.First().id].DisplayName;
                saveFileDialog1.FileName = filename;
                saveFileDialog1.Title = "Select Save Fileneme for Download";
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return null;
                return AmazonDriveControl.downloadItems(target, saveFileDialog1.FileName);
            }
        }

        private void downloadItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            downloadItems(listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.Info));
        }

        private void sendUDPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlayFiles(PlayOneTSFile, "Send UDP");
        }

        private JobControler.Job DoSearch()
        {
            string search_str = comboBox_FindStr.Text;
            IEnumerable<ItemInfo> selection = DriveData.AmazonDriveTree.Values;

            if (checkBox_File.Checked && checkBox_Folder.Checked)
                selection = selection.Where(x => x.Info.kind != "ASSET");
            else if (checkBox_Folder.Checked)
                selection = selection.Where(x => x.Info.kind == "FOLDER");
            else if (checkBox_File.Checked)
                selection = selection.Where(x => x.Info.kind != "FOLDER" && x.Info.kind != "ASSET");
            else
            {
                // all item selected
            }

            bool RegexFlag = checkBox_Regex.Checked;
            bool CaseFlag = checkBox_findCaseSensitive.Checked;
            bool SizeOver = checkBox_sizeOver.Checked;
            bool SizeUnder = checkBox_sizeUnder.Checked;
            decimal Over = numericUpDown_sizeOver.Value;
            decimal Under = numericUpDown_sizeUnder.Value;
            bool CreateTime = radioButton_createTime.Checked;
            bool ModifiedDate = radioButton_modifiedDate.Checked;
            DateTime from = dateTimePicker_from.Value;
            DateTime to = dateTimePicker_to.Value;
            bool fromflag = checkBox_dateFrom.Checked;
            bool toflag = checkBox_dateTo.Checked;


            var job = JobControler.CreateNewJob();
            job.DisplayName = "Search";
            var ct = job.ct;
            JobControler.Run(job, (j) =>
            {
                job.ProgressStr = "Create index...";
                job.Progress = -1;

                Parallel.ForEach(selection, (item) =>
                {
                    if (ct.IsCancellationRequested) return;
                    var disp = item.DisplayName;
                });

                if (RegexFlag)
                {
                    if (CaseFlag)
                        selection = selection.Where(x => Regex.IsMatch(x.DisplayName ?? "", search_str));
                    else
                        selection = selection.Where(x => Regex.IsMatch(x.DisplayName ?? "", search_str, RegexOptions.IgnoreCase));
                }
                else
                {
                    if (CaseFlag)
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

                if (SizeOver)
                    selection = selection.Where(x => (x.Info.contentProperties?.size ?? 0) > Over);
                if (SizeUnder)
                    selection = selection.Where(x => (x.Info.contentProperties?.size ?? 0) < Under);

                if (CreateTime)
                {
                    if (fromflag)
                        selection = selection.Where(x => x.Info.createdDate > from);
                    if (toflag)
                        selection = selection.Where(x => x.Info.createdDate < to);
                }
                if (ModifiedDate)
                {
                    if (fromflag)
                        selection = selection.Where(x => x.Info.modifiedDate > from);
                    if (toflag)
                        selection = selection.Where(x => x.Info.modifiedDate < to);
                }

                job.ProgressStr = "Searching...";

                var result = selection.ToArray();

                job.Progress = 1;
                job.ProgressStr = "Found : " + result.Length.ToString();

                synchronizationContext.Post((o) =>
                {
                    listviewitem.SearchResult = result;
                    listView1.VirtualListSize = listviewitem.Count;
                }, null);
            });
            return job;
        }

        private void button_search_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            if (comboBox_FindStr.Items.IndexOf(comboBox_FindStr.Text) < 0)
                comboBox_FindStr.Items.Add(comboBox_FindStr.Text);

            DoSearch();
        }

        private void button_mkdir_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("make Folder Start.");
            if (!initialized) return;

            string parent_id = listviewitem.Root?.Info.id;
            if (parent_id == null) return;

            var newname = textBox_newName.Text;

            var job = JobControler.CreateNewJob();
            job.DisplayName = "Create Folder: " + newname;
            job.ProgressStr = "wait for create folder";
            JobControler.Run(job, (j) =>
            {
                job.ProgressStr = "Create folder...";
                job.Progress = -1;
                AmazonDriveControl.CreateDirectory(newname, parent_id);
                job.ProgressStr = "done.";
                job.Progress = 1;
            });

            var wait1 = JobControler.CreateNewJob(type: JobControler.JobClass.WaitChanges, depends: job);
            wait1.DisplayName = "Wait changes";
            wait1.DoAlways = true;
            var ct1 = wait1.ct;
            var checkpoint = DriveData.ChangeCheckpoint;
            JobControler.Run(wait1, (j) =>
            {
                Task.Delay(TimeSpan.FromSeconds(3)).Wait(ct1);
                DriveData.GetChanges(checkpoint, ct1).Wait(ct1);
                ReloadItems(parent_id);
            });
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Config.Log.LogOut("rename Start.");
            if (!initialized) return;

            var selectItem = listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.Info);

            int f_all = selectItem.Count();
            int changecount = 0;
            if (f_all == 0) return;

            if (f_all > 1)
                if (MessageBox.Show(Resource_text.RenameMulti_str, "Rename Items", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            var checkpoint = DriveData.ChangeCheckpoint;
            string parent_id = listviewitem.Root?.Info.id;
            JobControler.Job prevjob = null;
            foreach (var downitem in selectItem)
            {
                if (DriveData.AmazonDriveTree[downitem.id].IsEncrypted == CryptMethods.Method1_CTR)
                    continue;

                var oldname = DriveData.AmazonDriveTree[downitem.id].DisplayName;

                var job = JobControler.CreateNewJob(JobControler.JobClass.Normal, depends: prevjob);
                prevjob = job;
                job.DisplayName = "Rename";
                job.ProgressStr = "wait for rename " + oldname;
                var ct = job.ct;
                JobControler.Run(job, (j) =>
                {
                    job.ProgressStr = "Rename... " + oldname;
                    job.Progress = -1;

                    string newfilename = null;

                    synchronizationContext.Send((o) =>
                    {
                        using (var NewName = new FormInputName())
                        {
                            NewName.NewItemName = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                            if (NewName.ShowDialog() != DialogResult.OK)
                                newfilename = null;
                            else
                                newfilename = NewName.NewItemName;
                        }
                    }, null);

                    if (string.IsNullOrEmpty(newfilename))
                        job.Cancel();
                    ct.ThrowIfCancellationRequested();
                    if (DriveData.AmazonDriveTree[downitem.id].IsEncrypted == CryptMethods.Method2_CBC_CarotDAV)
                    {
                        newfilename = CryptCarotDAV.EncryptFilename(newfilename);
                    }
                    ct.ThrowIfCancellationRequested();
                    changecount++;
                    Drive.renameItem(downitem.id, newfilename, ct: ct).Wait(ct);

                    job.Progress = 1;
                    job.ProgressStr = "Rename done. " + newfilename;
                });
            }

            var wait1 = JobControler.CreateNewJob(type: JobControler.JobClass.WaitChanges, depends: prevjob);
            wait1.DisplayName = "Wait changes";
            wait1.DoAlways = true;
            var ct1 = wait1.ct;
            JobControler.Run(wait1, (j) =>
            {
                DriveData.GetChanges(checkpoint, ct1).Wait(ct1);
                ReloadItems(parent_id);
            });

            Config.Log.LogOut("rename : done.");
        }

        private async void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (listviewitem.Root != null && (listView1.SelectedIndices.Contains(0) || listView1.SelectedIndices.Contains(1)))
                return;
            ClipboardAmazonDrive data = null;
            var items = listviewitem.GetItems(listView1.SelectedIndices);
            await Task.Run(() =>
            {
                data = new ClipboardAmazonDrive(items);
            });
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

                if (!listviewitem.Contains(droptarget?.Info.id) || droptarget?.Info.kind != "FOLDER")
                {
                    // display root is target
                }
                else
                {
                    current = droptarget;
                }

                if (current != null)
                {
                    if (current.Info.kind == "FOLDER")
                    {
                        if (e.Data.GetDataPresent(DataFormats.FileDrop))
                            e.Effect = DragDropEffects.Copy;
                        else
                        {
                            var selectedItems = GetSelectedItemsFromDataObject(e.Data);
                            if ((!selectedItems?.Select(x => x.id).Contains(current.Info.id) ?? false) && !current.children.Keys.Intersect(selectedItems?.Select(x => x.id)).Any())
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

        private void DragDrop_AmazonItem(System.Windows.Forms.IDataObject data, string toParent, string logprefix = "")
        {
            Config.Log.LogOut(string.Format("move({0}) Start.", logprefix));

            string parent_id = listviewitem.Root?.Info.id;
            var selects = GetSelectedItemsFromDataObject(data);
            int count = 0;

            var joblist = new List<JobControler.Job>();
            foreach (var aItem in selects)
            {
                var job = JobControler.CreateNewJob(type: JobControler.JobClass.Trash);
                job.DisplayName = DriveData.GetFullPathfromId(aItem.id);
                job.ProgressStr = "wait for move.";
                joblist.Add(job);
                var ct = job.ct;
                JobControler.Run(job, (j) =>
                {
                    job.ProgressStr = string.Format("Move Item... {0}/{1}", ++count, selects.Length);
                    job.Progress = -1;
                    var fromParent = aItem.parents[0];
                    var childid = aItem.id;

                    Drive.moveChild(childid, fromParent, toParent, ct).Wait(ct);
                    job.Progress = 1;
                    job.ProgressStr = "Move done.";
                });
            }

            ReloadAfterJob(joblist.ToArray(), parent_id);
        }

        private void DragDrop_FileDrop(string[] drags, string parent_id, string logprefix = "")
        {
            string disp_id = listviewitem.Root?.Info.id;

            var job = JobControler.CreateNewJob();
            job.DisplayName = "Drop Items";
            job.ProgressStr = "Searching...";
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                string[] dir_drags = null;
                dir_drags = drags.Where(x => Directory.Exists(x)).ToArray();
                drags = drags.Where(x => File.Exists(x)).ToArray();

                var upjob = AmazonDriveControl.DoFileUpload(drags, parent_id, WeekDepend: true, parentJob: job);
                if (upjob == null)
                {
                    upjob = AmazonDriveControl.DoDirectoryUpload(dir_drags, parent_id, WeekDepend: true, parentJob: job);
                }
                else
                {
                    var up2job = AmazonDriveControl.DoDirectoryUpload(dir_drags, parent_id, WeekDepend: true, parentJob: job);
                    if(up2job != null)
                        upjob = upjob.Concat(up2job).ToArray();
                }
                ReloadAfterJob(upjob, disp_id);
                job.Progress = 1;
                job.ProgressStr = "done.";
            });
        }

        private void listView_DragDrop_FileDrop(System.Windows.Forms.IDataObject data, string parent_id)
        {
            Config.Log.LogOut("upload(listview) Start.");
            string[] drags = (string[])data.GetData(DataFormats.FileDrop);

            if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            DragDrop_FileDrop(drags, parent_id, "listview");
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Point p = listView1.PointToClient(new Point(e.X, e.Y));
                ListViewItem item = listView1.GetItemAt(p.X, p.Y);
                var droptarget = item?.Tag as ItemInfo;
                var current = listviewitem.Root;

                if (!listviewitem.Contains(droptarget?.Info.id) || droptarget?.Info.kind != "FOLDER")
                {
                    // display root is target
                }
                else
                {
                    current = droptarget;
                }
                if (current == null) return;

                var ParentId = current.Info.id;
                if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS))
                {
                    DragDrop_AmazonItem(e.Data, ParentId, "listview");
                }
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    listView_DragDrop_FileDrop(e.Data, ParentId);
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
                var children_kind = item.Nodes.OfType<TreeNode>().Select(x => (x.Tag as ItemInfo).Info.kind);
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

                if (item == null || !string.IsNullOrEmpty((item.Tag as ItemInfo).Info.kind))
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
                            while ((item.Tag as ItemInfo).Info.kind != "FOLDER")
                            {
                                item = item.Parent;
                                if (item == null) break;
                            }
                        }
                        var toParent = (item?.Tag as ItemInfo)?.Info.id ?? DriveData.AmazonDriveRootID;
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

        private void treeView1_DragDrop_FileDrop(System.Windows.Forms.IDataObject data, string parent_id)
        {
            string disp_id = listviewitem.Root?.Info.id;

            Config.Log.LogOut("upload(treeview) Start.");
            string[] drags = (string[])data.GetData(DataFormats.FileDrop);

            if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            DragDrop_FileDrop(drags, parent_id, "treeview");
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Point p = treeView1.PointToClient(new Point(e.X, e.Y));
                TreeNode item = treeView1.GetNodeAt(p.X, p.Y);

                if (item != null)
                {
                    while ((item.Tag as ItemInfo)?.Info.kind != "FOLDER")
                    {
                        item = item.Parent;
                        if (item == null) break;
                    }
                }


                string ParentId = (item?.Tag as ItemInfo)?.Info.id ?? DriveData.AmazonDriveRootID;
                if (e.Data.GetDataPresent(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS))
                {
                    DragDrop_AmazonItem(e.Data, ParentId, "treeview");
                }
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    treeView1_DragDrop_FileDrop(e.Data, ParentId);
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
                    for (int i = 0; i < listviewitem.Items.Length; i++)
                        listView1.SelectedIndices.Add(i);
                }
                else
                {
                    for (int i = 2; i < listviewitem.Items.Length + 2; i++)
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
            if (listView1.SelectedIndices.Count == 0)
                return listviewitem.Items.Select(x => x.Info);
            if (listviewitem.Root == null)
                return listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.Info);

            if (listView1.SelectedIndices.Contains(0)) listView1.SelectedIndices.Remove(0);
            if (listView1.SelectedIndices.Contains(1)) listView1.SelectedIndices.Remove(1);

            return listviewitem.GetItems(listView1.SelectedIndices).Select(x => x.Info);
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

        private void button_Play_Click(object sender, EventArgs e)
        {
            PlayFiles(PlayOneTSFile, "Send UDP");
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

        private void PlayOneTSFile(FileMetadata_Info downitem, string download_str, JobControler.Job master, dynamic data)
        {
            var ct = master.ct;
            long bytePerSec = 0;
            long? SkipByte = null;
            DateTime InitialTOT = default(DateTime);

            var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
            synchronizationContext.Post((o) =>
            {
                label_sendname.Text = filename;
                trackBar_Pos.Tag = 1;
                trackBar_Pos.Minimum = 0;
                trackBar_Pos.Maximum = (int)(downitem.contentProperties.size / (10 / 8 * 1024 * 1024));
                trackBar_Pos.Value = 0;
                trackBar_Pos.Tag = 0;
            }, null);

            while (true)
            {
                synchronizationContext.Post((o) =>
                {
                    PressKeyForOtherApp();
                }, null);

                master.ProgressStr = download_str;
                master.Progress = 0;

                var internalToken = seekUDP_ct_source.Token;
                var externalToken = ct;
                try
                {
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(internalToken, externalToken))
                    {
                        Drive.downloadFile(downitem, SkipByte, ct: linkedCts.Token).ContinueWith((task) =>
                        {
                            using (var ret = task.Result)
                            {
                                using (var bufst = new BufferedStream(ret, ConfigAPI.CopyBufferSize))
                                using (var f = new PositionStream(bufst, downitem.contentProperties.size.Value, SkipByte))
                                {
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        master.ProgressStr = download_str + evnt.Log;
                                        master.Progress = (double)evnt.Position / evnt.Length;
                                    };
                                    using (var UDP = new UDP_TS_Stream(linkedCts.Token))
                                    {
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
                                        f.CopyToAsync(UDP, ConfigAPI.CopyBufferSize, linkedCts.Token).Wait(linkedCts.Token);
                                    }
                                }
                            }
                        }, linkedCts.Token).Wait(linkedCts.Token);
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

        private void PlayWithFFmpeg()
        {
            if (!initialized) return;

            var asm = Assembly.LoadFrom("ffmodule.dll");
            var typeInfo = asm.GetType("ffmodule.FFplayer");

            dynamic Player = Activator.CreateInstance(typeInfo);
            var logger = Stream.Synchronized(new LogWindowStream(Config.Log));
            var logwriter = TextWriter.Synchronized(new StreamWriter(logger));

            ffplayer = Player;
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
            var job = PlayFiles(new PlayOneFileDelegate(PlayOneFFmpegPlayer), "FFmpeg", data: Player);
            if (job == null) return;
            (job as JobControler.Job).DoAlways = true;
            JobControler.Run(job as JobControler.Job, (j) =>
            {
                Player.SetLogger(null);

                Config.FFmodule_fullscreen = Player.Fullscreen;
                Config.FFmodule_display = Player.Display;
                Config.FFmodule_mute = Player.Mute;
                Config.FFmodule_volume = Player.Volume;
                Config.FFmodule_width = Player.ScreenWidth;
                Config.FFmodule_hight = Player.ScreenHeight;
                Config.FFmodule_x = Player.ScreenXPos;
                Config.FFmodule_y = Player.ScreenYPos;
                ffplayer = null;

                (j as JobControler.Job).Progress = 1;
                (j as JobControler.Job).ProgressStr = "done.";
            });
        }

        private void button_FFplay_Click(object sender, EventArgs e)
        {
            PlayWithFFmpeg();
        }

        private void playWithFFplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlayWithFFmpeg();
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
                    var ext = Path.GetExtension(t.Value.DisplayName).ToLower();
                    foreach (var ici in decoders)
                    {
                        bool found = false;
                        var decext = ici.FilenameExtension.Split(';').Select(x => Path.GetExtension(x).ToLower()).ToArray();
                        if (decext.Contains(ext))
                        {
                            CancellationToken ct = player.ct;
                            using (var st = new AmazonDriveSeekableStream(Drive, t.Value.Info))
                            {
                                var img = Image.FromStream(st);
                                ret = new Bitmap(img);
                                found = true;
                            }
                            if (found) return ret;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Config.Log.LogOut(ex.ToString());
            }
            return null;
        }

        double FFplayStartDelay = double.NaN;
        double FFplayDuration = double.NaN;

        private void PlayOneFFmpegPlayer(FileMetadata_Info downitem, string download_str, JobControler.Job master, dynamic data)
        {
            string filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
            var Player = data;
            synchronizationContext.Post((o) =>
            {
                Player.StartSkip = FFplayStartDelay;
                Player.StopDuration = FFplayDuration;
                label_FFplay_sendname.Text = filename;
                timer3.Enabled = true;
            }, null);

            using (var driveStream = new AmazonDriveSeekableStream(Drive, downitem))
            using (var PosStream = new PositionStream(driveStream))
            {
                PosStream.PosChangeEvent += (src, evnt) =>
                {
                    master.Progress = (double)evnt.Position / evnt.Length;
                    master.ProgressStr = download_str + evnt.Log;
                };
                Player.Tag = downitem;
                var ct = master.ct;
                if (Player.Play(PosStream, filename, ct) != 0)
                    throw new OperationCanceledException("player cancel");
            }
            synchronizationContext.Post((o) =>
            {
                timer3.Enabled = false;
                label_FFplay_sendname.Text = "Play Filename";
            }, null);
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

        private delegate void PlayOneFileDelegate(FileMetadata_Info downitem, string download_str, JobControler.Job master, dynamic data);

        private JobControler.Job PlayFiles(PlayOneFileDelegate func, string LogPrefix, dynamic data = null)
        {
            Config.Log.LogOut(LogPrefix + " media files Start.");
            if (!initialized) return null;
            var select = listView1.SelectedIndices;
            if (select.Count == 0) return null;

            var selectItem = listviewitem.GetItems(select).Select(x => x.Info).Where(x => x.kind != "FOLDER").ToArray();

            int f_all = selectItem.Count();
            if (f_all == 0) return null;

            int f_cur = 0;
            var job = JobControler.CreateNewJob(JobControler.JobClass.Play);
            job.DisplayName = "Play files";
            job.ProgressStr = "wait for play";
            var ct = job.ct;
            JobControler.Job prevjob = job;
            nextUDPcount = 0;
            foreach (var downitem in selectItem)
            {
                var cjob = JobControler.CreateNewJob(JobControler.JobClass.PlayDownload, null, prevjob, job);
                if (prevjob == job) cjob.WeekDepend = true;
                prevjob = cjob;
                var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                cjob.DisplayName = "Play " + filename;
                cjob.ProgressStr = "wait for play";
                JobControler.Run(cjob, (j2) =>
                {
                    cjob.ct.ThrowIfCancellationRequested();
                    Config.Log.LogOut(LogPrefix + " download : " + filename);
                    var download_str = (f_all > 1) ? string.Format("Download({0}/{1})...", ++f_cur, f_all) : "Download...";

                    if (downitem.contentProperties.size > ConfigAPI.FilenameChangeTrickSize && !Regex.IsMatch(downitem.name, "^[\x20-\x7e]*$"))
                    {
                        Config.Log.LogOut(LogPrefix + " download : <BIG FILE> temporary filename change");
                        Interlocked.Increment(ref Config.AmazonDriveTempCount);
                        try
                        {
                            Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id).Wait(ct);
                            try
                            {
                                func(downitem, download_str, cjob, data);
                            }
                            finally
                            {
                                Drive.renameItem(downitem.id, downitem.name).Wait();
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref Config.AmazonDriveTempCount);
                        }
                    }
                    else
                    {
                        func(downitem, download_str, cjob, data);
                    }

                    Config.Log.LogOut(LogPrefix + " download : done.");
                    cjob.ProgressStr = "Play done.";
                    cjob.Progress = 1;
                });
            }
            JobControler.Run(job, (j) =>
            {
                job.ProgressStr = "play";
                job.Progress = 1;
            });
            var afterjob = JobControler.CreateNewJob(JobControler.JobClass.Clean, depends: prevjob);
            afterjob.DisplayName = "Clean up";
            return afterjob;
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

        private void button_Playbreak_Click(object sender, EventArgs e)
        {
            JobControler.CancelPlay();
        }

        private void buttonFFmpegmoduleConfig_Click(object sender, EventArgs e)
        {
            var form = new FormFFmoduleConfig();
            form.ShowDialog();
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

        private void checkBox_LockPassword2_CheckedChanged(object sender, EventArgs e)
        {
            Config.LockPassword2 = checkBox_LockPassword2.Checked;
            textBox_Password2.Enabled = !checkBox_LockPassword2.Checked;
            textBox_Password2.PasswordChar = (Config.LockPassword2) ? '*' : '\0';
        }

        private void checkBox_crypt_CheckedChanged(object sender, EventArgs e)
        {
            Config.UseEncryption = checkBox_crypt.Checked;
            if (!Config.UseEncryption)
                checkBox_cryptfilename.Checked = false;
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

        private void MakeTempDownloadLinks(IEnumerable<FileMetadata_Info> selectItem, JobControler.Job prevjob = null)
        {

            Config.Log.LogOut("MakeTempDownloadLinks : start");
            int f_all = selectItem.Count();
            if (f_all == 0) return;

            var templinks = new List<FileMetadata_Info>();
            int f_cur = 0;
            var joblist = new List<JobControler.Job>();
            foreach (var item in selectItem)
            {
                var job = JobControler.CreateNewJob(JobControler.JobClass.Normal, depends: prevjob);
                prevjob = job;
                joblist.Add(job);
                var ct = job.ct;
                job.DisplayName = "Make temporary link: " + DriveData.GetFullPathfromId(item.id);
                job.ProgressStr = "wait for makeTempLink";
                JobControler.Run(job, (j) =>
                {
                    job.Progress = -1;
                    job.ProgressStr = (f_all > 1) ? string.Format("Make temporary link({0}/{1})...", ++f_cur, f_all) : "Make temporary link...";
                    int retry = 6;
                    while (--retry > 0)
                    {
                        try
                        {
                            Drive.GetFileMetadata(item.id, ct: ct, templink: true).ContinueWith((task) =>
                            {
                                var newitem = task.Result;
                                templinks.Add(newitem);
                            }, ct).Wait(ct);
                            Config.Log.LogOut("MakeTempLink : " + item.name);
                            job.Progress = 1;
                            job.ProgressStr = "done.";
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
                    if(retry == 0)
                    {
                        Config.Log.LogOut("MakeTempLink failed: " + item.name);
                        JobControler.ErrorOut("MakeTempLink failed: " + item.name);
                        job.Error("MakeTempLink failed.");
                    }
                });
            }
            var afterjob = JobControler.CreateNewJob(JobControler.JobClass.Normal, depends: joblist.ToArray());
            afterjob.DisplayName = "Make temporary link result";
            afterjob.DoAlways = true;
            JobControler.Run(afterjob, (j) =>
            {
                afterjob.Progress = 1;
                afterjob.ProgressStr = "done.";
                synchronizationContext.Post((o) =>
                {
                    if (templinks.Count > 1)
                    {
                        var logform = new FormTemplink();
                        logform.TempLinks = templinks;
                        logform.ShowDialog();
                    }
                    else if (templinks.Count == 1)
                    {
                        Clipboard.SetText(templinks[0].tempLink);
                    }
                }, null);
            });
        }

        private void makeTemporaryLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!initialized) return;
            var select = listView1.SelectedIndices;
            if (select.Count == 0) return;

            var selectItem = listviewitem.GetItems(select).Select(x => x.Info).Where(x => x.kind != "FOLDER").ToArray();
            MakeTempDownloadLinks(selectItem);
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
            if (radioButton_crypt_1_CTR.Checked)
            {
                Config.CryptMethod = CryptMethods.Method1_CTR;
                tableLayoutPanel_password2.Enabled = false;
            }
        }

        private void radioButton_crypt_2_CBC_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_crypt_2_CBC.Checked)
            {
                Config.CryptMethod = CryptMethods.Method2_CBC_CarotDAV;
                checkBox_cryptfilename.Checked = true;
                tableLayoutPanel_password2.Enabled = false;
            }
        }

        private void radioButton_crypt_3_Rclone_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_crypt_3_Rclone.Checked)
            {
                Config.CryptMethod = CryptMethods.Method3_Rclone;
                tableLayoutPanel_password2.Enabled = true;
            }
        }

        private void checkBox_decodeView_CheckedChanged(object sender, EventArgs e)
        {
            Config.AutoDecode = checkBox_decodeView.Checked;
            ReloadItems(listviewitem.Root?.Info.id);
        }

        private void comboBox_CarotDAV_Escape_SelectedIndexChanged(object sender, EventArgs e)
        {
            Config.CarotDAV_CryptNameHeader = comboBox_CarotDAV_Escape.Text;
            if (!initialized) return;
            var job = JobControler.CreateNewJob();
            job.DisplayName = "Apply changes";
            job.ProgressStr = "CarotDAV CryptHeader change.";
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                DriveData.ChangeCryption2().Wait(job.ct);
                job.Progress = 1;
                job.ProgressStr = "done.";
                ReloadItems(listviewitem.Root?.Info.id);
            });
        }

        private void textBox_Password_Leave(object sender, EventArgs e)
        {
            if (Config.DrivePassword == textBox_Password.Text) return;

            Config.DrivePassword = textBox_Password.Text;
            if (!initialized) return;
            var job = JobControler.CreateNewJob();
            job.DisplayName = "Apply changes";
            job.ProgressStr = "passward change.";
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                DriveData.ChangeCryption1().Wait(job.ct);
                job.Progress = 1;
                job.ProgressStr = "done.";
                ReloadItems(listviewitem.Root?.Info.id);
            });
        }

        private void textBox_Password2_Leave(object sender, EventArgs e)
        {
            if (Config.DrivePassword2 == textBox_Password2.Text) return;

            Config.DrivePassword2 = textBox_Password2.Text;
            if (!initialized) return;
            var job = JobControler.CreateNewJob();
            job.DisplayName = "Apply changes";
            job.ProgressStr = "passward2 change.";
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                DriveData.ChangeCryption3().Wait(job.ct);
                job.Progress = 1;
                job.ProgressStr = "done.";
                ReloadItems(listviewitem.Root?.Info.id);
            });
        }

        private void textBox_Rclone_cryptroot_Leave(object sender, EventArgs e)
        {
            if (Config.CryptRoot == textBox_Rclone_cryptroot.Text) return;

            Config.CryptRoot = textBox_Rclone_cryptroot.Text;
            if (!initialized) return;
            var job = JobControler.CreateNewJob();
            job.DisplayName = "Apply changes";
            job.ProgressStr = "crypt root change.";
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                DriveData.ChangeCryption3().Wait(job.ct);
                job.Progress = 1;
                job.ProgressStr = "done.";
                ReloadItems(listviewitem.Root?.Info.id);
            });
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

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selection = listviewitem.GetItems(listView1.SelectedIndices).ToArray();
            var current = listviewitem.Root;
            if (selection.Count() == 1)
            {
                var select = selection.First();
                if (select.Info?.kind == "FOLDER")
                    current = select;
            }
            if (current == null) return;

            var ParentId = current.Info.id;
            if (Clipboard.ContainsData(DataFormats.FileDrop))
            {
                Config.Log.LogOut("upload(clipboard) Start.");
                string[] drags = (string[])Clipboard.GetData(DataFormats.FileDrop);

                if (drags.Where(x => Directory.Exists(x)).Count() > 0)
                    if (MessageBox.Show(Resource_text.UploadFolder_str, "Folder upload", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

                DragDrop_FileDrop(drags, ParentId, "clipboard");
                return;
            }
            System.Windows.Forms.IDataObject data = Clipboard.GetDataObject();
            var formats = data.GetFormats();
            if (formats.Contains(ClipboardAmazonDrive.CFSTR_AMAZON_DRIVE_ITEMS))
            {
                DragDrop_AmazonItem(data, ParentId, "clipboard");
            }
        }

        private void checkBox_UploadTrick_CheckedChanged(object sender, EventArgs e)
        {
            Config.UploadTrick1 = checkBox_UploadTrick.Checked;
        }

        private void checkBox_upSkip_CheckedChanged(object sender, EventArgs e)
        {
            AmazonDriveControl.upskip_check = checkBox_upSkip.Checked;
        }

        private void checkBox_MD5_CheckedChanged(object sender, EventArgs e)
        {
            AmazonDriveControl.checkMD5 = checkBox_MD5.Checked;
        }

        private void checkBox_overrideUpload_CheckedChanged(object sender, EventArgs e)
        {
            AmazonDriveControl.overrideConflict = checkBox_overrideUpload.Checked;
        }

        private void numericUpDown_ParallelUpload_ValueChanged(object sender, EventArgs e)
        {
            Config.ParallelUpload = (int)numericUpDown_ParallelUpload.Value;
        }

        private void numericUpDown_ParallelDownload_ValueChanged(object sender, EventArgs e)
        {
            Config.ParallelDownload = (int)numericUpDown_ParallelDownload.Value;
        }

        private void button_masterpass_Click(object sender, EventArgs e)
        {
            using (var f = new FormMasterPass())
                f.ShowDialog();
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            FormTaskList.Instance.FixPosition();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            FormTaskList.Instance.FixPosition();
        }

    }

    static class Extensions
    {
        public static IOrderedEnumerable<ItemInfo> SortByKind(this IEnumerable<ItemInfo> x, bool SortKind)
        {
            return x.OrderBy(y => (SortKind) ? (y.Info.kind != "FOLDER") : true).ThenBy(y => (SortKind)? y.IsEncrypted: CryptMethods.Unknown);
        }
    }
}

