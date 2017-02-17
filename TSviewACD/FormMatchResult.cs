using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormMatchResult : Form
    {
        public FormMatchResult()
        {
            InitializeComponent();
        }

        public IEnumerable<FormMatch.MatchItem> LocalOnly
        {
            set
            {
                foreach(var item in value)
                {
                    listBox_LocalOnly.Items.Add(item.local.path);
                }
            }
        }
        public IEnumerable<FormMatch.MatchItem> RemoteOnly
        {
            set
            {
                foreach (var item in value)
                {
                    listBox_RemoteOnly.Items.Add(item.remote.path);
                }
            }
        }
        public IEnumerable<FormMatch.MatchItem> Unmatch
        {
            set
            {
                foreach (var item in value)
                {
                    listView_Unmatch.Items.Add(new ListViewItem(new string[]{
                        item.local.path,
                        item.local.size.ToString(),
                        item.local.MD5,
                        item.remote.info.contentProperties?.md5,
                        item.remote.info.contentProperties?.size.ToString(),
                        item.remote.path,
                    }));
                }
            }
        }
        public IEnumerable<FormMatch.MatchItem> Match
        {
            set
            {
                foreach (var item in value)
                {
                    listView_Match.Items.Add(new ListViewItem(new string[]{
                        item.local.path,
                        item.remote.path,
                        item.local.size.ToString(),
                        item.local.MD5,
                    }));
                }
            }
        }
        public IDictionary<string, FormMatch.LocalItemInfo[]> LocalDup
        {
            set
            {
                foreach(var item in value)
                {
                    var node = treeView_localDup.Nodes.Add(item.Key);
                    foreach(var ditem in item.Value)
                    {
                        if(ditem.MD5 == null)
                            node.Nodes.Add(string.Format("size:{0} {1}", ditem.size, ditem.path));
                        else
                            node.Nodes.Add(string.Format("size:{0} MD5:{1} {2}", ditem.size, ditem.MD5, ditem.path));
                    }
                }
            }
        }
        public IDictionary<string, FormMatch.RemoteItemInfo[]> RemoteDup
        {
            set
            {
                foreach (var item in value)
                {
                    var node = treeView_remoteDup.Nodes.Add(item.Key);
                    foreach (var ditem in item.Value)
                    {
                        if (ditem.info.contentProperties?.md5 == null)
                            node.Nodes.Add(string.Format("size:{0} {1}", ditem.info.contentProperties?.size, ditem.path));
                        else
                            node.Nodes.Add(string.Format("size:{0} MD5:{1} {2}", ditem.info.contentProperties?.size, ditem.info.contentProperties?.md5, ditem.path));
                    }
                }
            }
        }
    }
}
