using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormDriveTree : Form
    {
        public FormDriveTree()
        {
            InitializeComponent();
            LoadImage();
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
            }

            Win32.IImageList imglist = null;
            int rsult = Win32.SHGetImageList(Win32.SHIL_EXTRALARGE, ref Win32.IID_IImageList, out imglist);

            IntPtr hicon = IntPtr.Zero;

            Win32.SHGetStockIconInfo(Win32.SIID_STUFFEDFOLDER, Win32.SHGSI_ICON, ref sii);
            if (sii.hIcon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(sii.hIcon);
                imageList_icon.Images.Add("Folder2", aIcon);
            }

            Win32.SHGetStockIconInfo(Win32.SIID_DOCNOASSOC, Win32.SHGSI_ICON, ref sii);
            if (sii.hIcon != IntPtr.Zero)
            {
                Icon aIcon = Icon.FromHandle(sii.hIcon);
                imageList_icon.Images.Add("Doc", aIcon);
            }
            treeView1.ImageList = imageList_icon;
        }

        private TreeNode[] GenerateTreeNode(IEnumerable<ItemInfo> children, int count = 0)
        {
            return children.Select(x =>
            {
                int img = (x.info.kind == "FOLDER") ? 0 : 2;
                var node = new TreeNode(x.DisplayName, img, img);
                node.Name = x.DisplayName;
                node.Tag = x;
                if (x.info.kind == "FOLDER" && count > 0)
                {
                    node.Nodes.AddRange(GenerateTreeNode(x.children.Values, count - 1));
                }
                return node;
            }).ToArray();
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

        public ItemInfo root
        {
            set
            {
                var items = GenerateTreeNode(value.children.Values, 1);
                treeView1.Nodes.Clear();
                var root = treeView1.Nodes.Add("/", "/", 0, 0);
                root.Tag = value;
                root.Nodes.AddRange(items);
                root.Expand();
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            LoadTreeItem(e.Node);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var path = e.Node.FullPath;
            textBox1.Text = (path == "/")? path: path.Substring(1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0) return;

            var paths = textBox1.Text.Split('/');
            var node = treeView1.Nodes[0];
            foreach(var p in paths)
            {
                if (p == "") continue;
                if (node.Nodes.ContainsKey(p))
                {
                    node = node.Nodes.Find(p, false).FirstOrDefault();
                    node.Expand();
                }
                else
                {
                    break;
                }
            }
            treeView1.SelectedNode = node;
        }

        public string SelectedID
        {
            get
            {
                return (treeView1.SelectedNode?.Tag as ItemInfo)?.info.id;
            }
        }
    }
}
