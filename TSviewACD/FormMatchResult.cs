using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
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
                listBox_LocalOnly.DataSource = value;
            }
        }
        public IEnumerable<FormMatch.MatchItem> RemoteOnly
        {
            set
            {
                listBox_RemoteOnly.DataSource = value;
            }
        }
        public IEnumerable<FormMatch.MatchItem> Unmatch
        {
            set
            {
                foreach (var item in value)
                {
                    var newitem = new ListViewItem(new string[]{
                        item.local.path,
                        item.local.size.ToString(),
                        item.local.MD5,
                        item.remote.info.contentProperties?.md5,
                        item.remote.info.contentProperties?.size.ToString(),
                        item.remote.path,
                    });
                    newitem.Tag = item;
                    listView_Unmatch.Items.Add(newitem);
                }
            }
        }
        public IEnumerable<FormMatch.MatchItem> Match
        {
            set
            {
                foreach (var item in value)
                {
                    var newitem = new ListViewItem(new string[]{
                        item.local.path,
                        item.remote.path,
                        item.local.size.ToString(),
                        item.local.MD5,
                    });
                    newitem.Tag = item;
                    listView_Match.Items.Add(newitem);
                }
            }
        }
        public IDictionary<string, FormMatch.LocalItemInfo[]> LocalDup
        {
            set
            {
                foreach (var item in value)
                {
                    var node = treeView_localDup.Nodes.Add(item.Key);
                    foreach (var ditem in item.Value)
                    {
                        TreeNode newitem;
                        if (ditem.MD5 == null)
                            newitem = new TreeNode(string.Format("size:{0} {1}", ditem.size, ditem.path));
                        else
                            newitem = new TreeNode(string.Format("size:{0} MD5:{1} {2}", ditem.size, ditem.MD5, ditem.path));
                        newitem.Tag = ditem;
                        node.Nodes.Add(newitem);
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
                        TreeNode newitem;
                        if (ditem.info.contentProperties?.md5 == null)
                            newitem = new TreeNode(string.Format("size:{0} {1}", ditem.info.contentProperties?.size, ditem.path));
                        else
                            newitem = new TreeNode(string.Format("size:{0} MD5:{1} {2}", ditem.info.contentProperties?.size, ditem.info.contentProperties?.md5, ditem.path));
                        newitem.Tag = ditem;
                        node.Nodes.Add(newitem);
                    }
                }
            }
        }

        private void listBox_LocalOnly_Format(object sender, ListControlConvertEventArgs e)
        {
            var item = e.ListItem as FormMatch.MatchItem;
            e.Value = item.local.path;
        }

        private void listBox_RemoteOnly_Format(object sender, ListControlConvertEventArgs e)
        {
            var item = e.ListItem as FormMatch.MatchItem;
            e.Value = item.remote.path;
        }

        delegate void SaveDataFunc(StreamWriter sw);

        private void SaveList(SaveDataFunc Func)
        {
            if (Func == null) return;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var filename = saveFileDialog1.FileName;
                using (var fs = new FileStream(filename, FileMode.Create))
                using (var sw = new StreamWriter(fs))
                {
                    Func(sw);
                }
            }
        }

        private void button_SaveLocalList_Click(object sender, EventArgs e)
        {
            SaveList(sw =>
            {
                if (listBox_LocalOnly.DataSource != null)
                {
                    foreach (var item in listBox_LocalOnly.DataSource as IEnumerable<FormMatch.MatchItem>)
                    {
                        sw.WriteLine(item.local.path);
                    }
                }
            });
        }

        private void button_SaveRemoteList_Click(object sender, EventArgs e)
        {
            SaveList(sw =>
            {
                if (listBox_RemoteOnly.DataSource != null)
                {
                    sw.WriteLine("Path,id,size,MD5");
                    foreach (var item in listBox_RemoteOnly.DataSource as IEnumerable<FormMatch.MatchItem>)
                    {
                        sw.WriteLine("{0},{1},{2},{3}",
                            item.remote.path,
                            item.remote.info.id,
                            item.remote.info.contentProperties?.size,
                            item.remote.info.contentProperties?.md5);
                    }
                }
            });
        }

        private void button_SaveUnmatchList_Click(object sender, EventArgs e)
        {
            if (listView_Unmatch.Items.Count == 0)
                return;
            SaveList(sw =>
            {
                sw.WriteLine("LocalPath,LocalSize,LocalMD5,RemotePath,RemoteSize,RemoteMD5,RemoteID");
                foreach (ListViewItem item in listView_Unmatch.Items)
                {
                    if (item.Tag != null)
                    {
                        var data = item.Tag as FormMatch.MatchItem;
                        sw.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                            data.local.path,
                            data.local.size,
                            data.local.MD5,
                            data.remote.path,
                            data.remote.info.contentProperties?.size,
                            data.remote.info.contentProperties?.md5,
                            data.remote.info.id);
                    }
                }
            });
        }

        private void button_SaveLocalDupList_Click(object sender, EventArgs e)
        {
            if (treeView_localDup.Nodes.Count == 0)
                return;
            SaveList(sw =>
            {
                foreach (TreeNode node1 in treeView_localDup.Nodes)
                {
                    sw.WriteLine(node1.Text);
                    foreach (TreeNode node2 in node1.Nodes)
                    {
                        var item = node2.Tag as FormMatch.LocalItemInfo;
                        sw.WriteLine("\t{0},{1},{2}",
                            item.path,
                            item.size,
                            item.MD5);
                    }
                }
            });
        }

        private void button_SaveRemoteDupList_Click(object sender, EventArgs e)
        {
            if (treeView_remoteDup.Nodes.Count == 0)
                return;
            SaveList(sw =>
            {
                foreach (TreeNode node1 in treeView_remoteDup.Nodes)
                {
                    sw.WriteLine(node1.Text);
                    foreach (TreeNode node2 in node1.Nodes)
                    {
                        var item = node2.Tag as FormMatch.RemoteItemInfo;
                        sw.WriteLine("\t{0},{1},{2},{3}",
                            item.path,
                            item.info.contentProperties?.size,
                            item.info.contentProperties?.md5,
                            item.info.id);
                    }
                }
            });
        }

        private void button_SaveMatchedList_Click(object sender, EventArgs e)
        {
            if (listView_Match.Items.Count == 0)
                return;
            SaveList(sw =>
            {
                sw.WriteLine("LocalPath,RemotePath,Size,MD5,RemoteID");
                foreach (ListViewItem item in listView_Match.Items)
                {
                    if (item.Tag != null)
                    {
                        var data = item.Tag as FormMatch.MatchItem;
                        sw.WriteLine("{0},{1},{2},{3},{4}",
                            data.local.path,
                            data.remote.path,
                            data.remote.info.contentProperties?.size,
                            data.remote.info.contentProperties?.md5,
                            data.remote.info.id);
                    }
                }
            });
        }

        private async void button_Upload_Click(object sender, EventArgs e)
        {
            button_Upload.Enabled = false;
            try
            {
                var items = listBox_LocalOnly.SelectedItems;
                if (items.Count == 0) return;

                // アップロード先を選択
                var tree = new FormDriveTree();
                tree.root = DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID];
                if (tree.ShowDialog() != DialogResult.OK)
                    return;
                var targetID = tree.SelectedID;
                if (string.IsNullOrEmpty(targetID)) return;

                // 対象がフォルダでない場合は、その上に上がる
                while (targetID != DriveData.AmazonDriveRootID && DriveData.AmazonDriveTree[targetID].info.kind != "FOLDER")
                {
                    targetID = DriveData.AmazonDriveTree[targetID].info.parents[0];
                }

                // アップロードファイルのパス共通部分を検索
                var pathbase = FormMatch.GetBasePath(items.Cast<FormMatch.MatchItem>().Select(x => x.local.path));
                bool createdir = items.Count > 1 && items.Cast<FormMatch.MatchItem>().GroupBy(x => Path.GetFileName(x.local.path)).Any(g => g.Count() > 1);
                var uploadfiles = items.Cast<FormMatch.MatchItem>().Select(x => x.local.path.Substring(pathbase.Length)).ToArray();

                var task = Program.MainForm.CreateTask("match");
                var ct = task.cts.Token;
                try
                {
                    foreach (var upfile in uploadfiles)
                    {
                        ct.ThrowIfCancellationRequested();
                        var parentID = targetID;
                        var filename = Path.Combine(pathbase, upfile);
                        if (createdir)
                        {
                            var path = upfile.Split('\\', '/');
                            // フォルダを確認してなければ作る
                            foreach (var p in path.Take(path.Length - 1))
                            {
                                if (p == "") continue;
                                if (DriveData.AmazonDriveTree[parentID].children.Values.Select(x => x.info.name).Contains(p))
                                {
                                    parentID = DriveData.AmazonDriveTree[parentID].children.Values.Where(x => x.info.name == p).Select(x => x.info.id).FirstOrDefault();
                                }
                                else
                                {
                                    int retry = 6;
                                    while (--retry > 0)
                                    {
                                        try
                                        {
                                            var checkpoint = DriveData.ChangeCheckpoint;
                                            // make subdirectory
                                            var newdir = await DriveData.Drive.createFolder(p, parentID, ct);
                                            var children = await DriveData.GetChanges(checkpoint, ct);
                                            if (children.Where(x => x.name.Contains(p)).LastOrDefault()?.status == "AVAILABLE")
                                            {
                                                Config.Log.LogOut("createFolder : child found.");
                                                parentID = newdir.id;
                                                break;
                                            }
                                            await Task.Delay(2000, ct);
                                        }
                                        catch (HttpRequestException)
                                        {
                                            //retry
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            throw;
                                        }
                                    }
                                }
                            }
                        }
                        // アップロード
                        var checkpoint2 = DriveData.ChangeCheckpoint;
                        if (await Program.MainForm.DoFileUpload(new string[] { filename }, parentID, ct: ct) < 0)
                            break;
                        await DriveData.GetChanges(checkpoint2, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    Program.MainForm.FinishTask(task);
                }
            }
            finally
            {
                button_Upload.Enabled = true;
            }
        }

        private async void button_Download_Click(object sender, EventArgs e)
        {
            button_Download.Enabled = false;
            try
            {
                var items = listBox_RemoteOnly.SelectedItems;
                if (items.Count == 0) return;

                await Program.MainForm.downloadItems(items.Cast<FormMatch.MatchItem>().Select(x => x.remote.info));
            }
            finally
            {
                button_Download.Enabled = true;
            }
        }

        private async void button_trash_Click(object sender, EventArgs e)
        {
            button_trash.Enabled = false;
            try
            {
                var items = listBox_RemoteOnly.SelectedItems;
                if (items.Count == 0) return;

                await Program.MainForm.DoTrashItem(items.Cast<FormMatch.MatchItem>().Select(x => x.remote.info.id));
            }
            finally
            {
                button_trash.Enabled = true;
            }
        }
    }
}
