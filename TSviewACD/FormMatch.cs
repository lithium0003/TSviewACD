using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public sealed partial class FormMatch : Form
    {
        private static readonly FormMatch _instance = new FormMatch();

        public static FormMatch Instance
        {
            get
            {
                return _instance;
            }
        }

        private FormMatch()
        {
            InitializeComponent();
            listBox_remote.DataSource = _SelectedRemoteFiles;
        }

        JobControler.Job runningJob;

        private IEnumerable<FileMetadata_Info> _SelectedRemoteFiles;

        public IEnumerable<FileMetadata_Info> SelectedRemoteFiles
        {
            get
            {
                return _SelectedRemoteFiles;
            }
            set
            {
                if (value == null)
                {
                    _SelectedRemoteFiles = null;
                    listBox_remote.DataSource = null;
                }
                else
                {
                    _SelectedRemoteFiles = value.ToArray()
                        .Select(x => DriveData.GetAllChildrenfromId(x.id))
                        .SelectMany(x => x.Select(y => y))
                        .Distinct()
                        .Where(x => x.kind != "ASSET")
                        .Where(x => x.kind != "FOLDER");
                    listBox_remote.DataSource = _SelectedRemoteFiles.ToList();
                }
            }
        }
        
        private void button_AddFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            
            listBox_local.Items.AddRange(openFileDialog1.FileNames.Where(x => listBox_local.Items.IndexOf(x) < 0).ToArray());
        }

        private void DoDirectoryAdd(IEnumerable<string> Filenames)
        {
            foreach (var filename in Filenames)
            {
                listBox_local.Items.AddRange(Directory.EnumerateFiles(filename).Where(x => listBox_local.Items.IndexOf(x) < 0).ToArray());

                DoDirectoryAdd(Directory.EnumerateDirectories(filename));
            }
        }

        private void button_AddFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;

            label_info.Text = "Add Folder ...";
            try
            {
                DoDirectoryAdd(new string[] { folderBrowserDialog1.SelectedPath });
            }
            catch { }
            label_info.Text = "";
        }

        private void deltetItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(var i in listBox_local.SelectedIndices.OfType<int>().Reverse())
            {
                listBox_local.Items.RemoveAt(i);
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.A | Keys.Control))
            {
                listBox_local.BeginUpdate();
                try
                {
                    for (var i = 0; i < listBox_local.Items.Count; i++)
                        listBox_local.SetSelected(i, true);
                }
                finally
                {
                    listBox_local.EndUpdate();
                }
            }
        }

        static public string GetBasePath(IEnumerable<string> paths)
        {
            string prefix = null;
            foreach(var p in paths)
            {
                if (prefix == null)
                {
                    var filename = Path.GetFileName(p);
                    prefix = p.Substring(0, p.Length - filename.Length);
                }
                if (prefix == "")
                    break;
                while (!p.StartsWith(prefix) && prefix != "")
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                    var filename = Path.GetFileName(prefix);
                    prefix = prefix.Substring(0, prefix.Length - filename.Length);
                }
            }
            return prefix ?? "";
        }

        public class LocalItemInfo
        {
            public string path;
            public string name;
            public long size;
            public string MD5;
            public LocalItemInfo(string path, string name, long size, string MD5)
            {
                this.path = path;
                this.name = name;
                this.size = size;
                this.MD5 = MD5;
            }
        }

        public class RemoteItemInfo
        {
            public FileMetadata_Info info;
            public string path;
            public string name;
            public RemoteItemInfo(FileMetadata_Info info, string path, string name)
            {
                this.info = info;
                this.path = path;
                this.name = name;
            }
        }

        public class MatchItem
        {
            public LocalItemInfo local;
            public RemoteItemInfo remote;
            public MatchItem(LocalItemInfo local, RemoteItemInfo remote)
            {
                this.local = local;
                this.remote = remote;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (SelectedRemoteFiles == null) return;

            List<MatchItem> RemoteOnly = new List<MatchItem>();
            List<MatchItem> LocalOnly = new List<MatchItem>();
            List<MatchItem> BothAndMatch = new List<MatchItem>();
            List<MatchItem> BothAndUnmatch = new List<MatchItem>();
            Dictionary<string, LocalItemInfo[]> LocalDup = new Dictionary<string, LocalItemInfo[]>();
            Dictionary<string, RemoteItemInfo[]> RemoteDup = new Dictionary<string, RemoteItemInfo[]>();

            var synchronizationContext = SynchronizationContext.Current;
            bool TreeFlag = radioButton_Tree.Checked;
            bool FilenameFlag = radioButton_filename.Checked;
            bool MD5Flag = radioButton_MD5.Checked;

            var job = JobControler.CreateNewJob(JobControler.JobClass.Normal);
            job.DisplayName = "Match";
            job.ProgressStr = "wait for run";
            runningJob = job;
            bool done = false;
            JobControler.Run(job, (j) =>
            {
                job.ProgressStr = "running...";
                job.Progress = -1;

                synchronizationContext.Post((o) =>
                {
                    button_start.Enabled = false;
                }, null);

                var remote = SelectedRemoteFiles.Select(x => new RemoteItemInfo(x, DriveData.GetFullPathfromId(x.id), null)).ToArray();
                var remotebasepath = GetBasePath(remote.Select(x => x.path));

                if (TreeFlag)
                    remote = remote.Select(x => new RemoteItemInfo(x.info, x.path, x.path.Substring(remotebasepath.Length))).ToArray();
                if (FilenameFlag)
                    remote = remote.Select(x => new RemoteItemInfo(x.info, x.path, DriveData.AmazonDriveTree[x.info.id].DisplayName)).ToArray();
                if (MD5Flag)
                    remote = remote.Select(x => new RemoteItemInfo(x.info, x.path, x.info.contentProperties?.md5)).ToArray();

                var localpath = listBox_local.Items.Cast<string>();
                var localbasepath = GetBasePath(localpath);
                var len = localpath.Count();
                int i = 0;
                foreach (var ritem in remote.GroupBy(x => x.name).Where(g => g.Count() > 1))
                {
                    RemoteDup[ritem.Key] = ritem.ToArray();
                }
                var local = ((radioButton_MD5.Checked) ?
                    (localpath.Select(x =>
                    {
                        byte[] md5 = null;
                        ++i;
                        synchronizationContext.Post((o) =>
                        {
                            if (runningJob?.ct.IsCancellationRequested ?? true) return;
                            label_info.Text = o as string;
                        }, string.Format("{0}/{1} Check file MD5...{2}", i, len, x));
                        using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                        using (var hfile = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            md5 = md5calc.ComputeHash(hfile);
                            var MD5 = BitConverter.ToString(md5).ToLower().Replace("-", "");
                            return new LocalItemInfo(x, MD5, hfile.Length, MD5);
                        }
                    })) :
                    (radioButton_Tree.Checked) ?
                        localpath.Select(x =>
                        {
                            if (checkBox_MD5.Checked)
                            {
                                byte[] md5 = null;
                                ++i;
                                synchronizationContext.Post((o) =>
                                {
                                    if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                    label_info.Text = o as string;
                                }, string.Format("{0}/{1} Check file MD5...{2}", i, len, x));
                                using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                                using (var hfile = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    md5 = md5calc.ComputeHash(hfile);
                                    var MD5 = BitConverter.ToString(md5).ToLower().Replace("-", "");
                                    return new LocalItemInfo(x, x.Substring(localbasepath.Length).Replace('\\', '/'), hfile.Length, MD5);
                                }
                            }
                            else
                            {
                                ++i;
                                synchronizationContext.Post((o) =>
                                {
                                    if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                    label_info.Text = o as string;
                                }, string.Format("{0}/{1} Check file ...{2}", i, len, x));
                                return new LocalItemInfo(x, x.Substring(localbasepath.Length).Replace('\\', '/'), new FileInfo(x).Length, null);
                            }
                        }) :
                        localpath.Select(x =>
                        {
                            if (checkBox_MD5.Checked)
                            {
                                byte[] md5 = null;
                                ++i;
                                synchronizationContext.Post((o) =>
                                {
                                    if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                    label_info.Text = o as string;
                                }, string.Format("{0}/{1} Check file MD5...{2}", i, len, x));
                                using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                                using (var hfile = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    md5 = md5calc.ComputeHash(hfile);
                                    var MD5 = BitConverter.ToString(md5).ToLower().Replace("-", "");
                                    return new LocalItemInfo(x, Path.GetFileName(x), hfile.Length, MD5);
                                }
                            }
                            else
                            {
                                ++i;
                                synchronizationContext.Post((o) =>
                                {
                                    if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                    label_info.Text = o as string;
                                }, string.Format("{0}/{1} Check file ...{2}", i, len, x));
                                return new LocalItemInfo(x, Path.GetFileName(x), new FileInfo(x).Length, null);
                            }
                        }))
                .GroupBy(x => x.name).ToArray();

                i = 0;
                foreach (var litem in local)
                {
                    job.ct.ThrowIfCancellationRequested();
                    var matchitem = remote.Where(x => x.name == litem.FirstOrDefault()?.name).ToArray();

                    if (litem.Count() > 1)
                    {
                        LocalDup[litem.Key] = litem.ToArray();
                    }

                    if (matchitem.Length > 0)
                    {
                        List<RemoteItemInfo> RemoteMatched = new List<RemoteItemInfo>();
                        List<LocalItemInfo> LocalUnMatched = new List<LocalItemInfo>();
                        // match test
                        foreach (var item in litem)
                        {
                            ++i;
                            synchronizationContext.Post((o) =>
                            {
                                if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                label_info.Text = o as string;
                            }, string.Format("{0}/{1} {2}", i, len, item.path));

                            List<RemoteItemInfo> Matched = new List<RemoteItemInfo>();
                            foreach (var ritem in matchitem)
                            {
                                if (item.size == ritem.info.contentProperties?.size)
                                {
                                    if (item.MD5 == null || item.MD5 == ritem.info.contentProperties?.md5)
                                    {
                                        Matched.Add(ritem);
                                    }
                                }
                            }

                            if (Matched.Count() == 0)
                            {
                                LocalUnMatched.Add(item);
                            }

                            BothAndMatch.AddRange(Matched.Select(x => new MatchItem(item, x)));
                            RemoteMatched.AddRange(Matched);
                        }

                        var RemoteUnMatched = matchitem.Except(RemoteMatched);
                        if (RemoteUnMatched.Count() < LocalUnMatched.Count())
                        {
                            BothAndUnmatch.AddRange(RemoteUnMatched.Concat(RemoteMatched).Zip(LocalUnMatched, (r, l) => new MatchItem(l, r)));
                        }
                        else if (RemoteUnMatched.Count() > LocalUnMatched.Count())
                        {
                            BothAndUnmatch.AddRange(LocalUnMatched.Concat(litem).Zip(RemoteUnMatched, (l, r) => new MatchItem(l, r)));
                        }
                        else
                        {
                            if (RemoteUnMatched.Count() > 0)
                                BothAndUnmatch.AddRange(LocalUnMatched.Zip(RemoteUnMatched, (l, r) => new MatchItem(l, r)));
                        }
                    }
                    else
                    {
                        //nomatch
                        foreach (var item in litem)
                        {
                            ++i;
                            synchronizationContext.Post((o) =>
                            {
                                if (runningJob?.ct.IsCancellationRequested ?? true) return;
                                label_info.Text = o as string;
                            }, string.Format("{0}/{1} {2}", i, len, item.path));
                            LocalOnly.Add(new MatchItem(item, null));
                        }
                    }
                }
                RemoteOnly.AddRange(remote.Select(x => x.info)
                    .Except(BothAndMatch.Select(x => x.remote.info))
                    .Except(BothAndUnmatch.Select(x => x.remote.info))
                    .Select(x => remote.Where(y => y.info == x).FirstOrDefault())
                    .Select(x => new MatchItem(null, x)));
                done = true;
                job.Progress = 1;
                job.ProgressStr = "done.";
            });
            var afterjob = JobControler.CreateNewJob(JobControler.JobClass.Clean, job);
            afterjob.DisplayName = "clean up";
            afterjob.DoAlways = true;
            JobControler.Run(afterjob, (j) =>
            {
                afterjob.ProgressStr = "done.";
                afterjob.Progress = 1;
                runningJob = null;
                synchronizationContext.Post((o) =>
                {
                    label_info.Text = "";
                    button_start.Enabled = true;
                    if (done)
                    {
                        var result = new FormMatchResult();
                        result.RemoteOnly = RemoteOnly;
                        result.LocalOnly = LocalOnly;
                        result.Unmatch = BothAndUnmatch;
                        result.Match = BothAndMatch;
                        result.RemoteDup = RemoteDup;
                        result.LocalDup = LocalDup;
                        result.Show();
                    }
                }, null);
            });
        }

        private void FormMatch_FormClosing(object sender, FormClosingEventArgs e)
        {
            runningJob?.Cancel();
            Hide();
            e.Cancel = true;
        }

        private void listBox_remote_Format(object sender, ListControlConvertEventArgs e)
        {
            e.Value = DriveData.GetFullPathfromId((e.ListItem as FileMetadata_Info).id);
        }

        private void button_AddRemote_Click(object sender, EventArgs e)
        {
            var items = SelectedRemoteFiles.ToList();
            items.AddRange(Program.MainForm.GetSeletctedRemoteFiles());
            SelectedRemoteFiles = items;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var items = SelectedRemoteFiles.ToList();
            foreach (var i in listBox_remote.SelectedIndices.OfType<int>().Reverse())
            {
                items.RemoveAt(i);
            }
            SelectedRemoteFiles = items;
        }

        private void listBox_remote_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.A | Keys.Control))
            {
                listBox_remote.BeginUpdate();
                try
                {
                    for (var i = 0; i < listBox_remote.Items.Count; i++)
                        listBox_remote.SetSelected(i, true);
                }
                finally
                {
                    listBox_remote.EndUpdate();
                }
            }
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            runningJob?.Cancel();
        }

        private void listBox_local_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listBox_local_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileName =
                (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (var item in fileName)
            {
                try
                {
                    if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
                        DoDirectoryAdd(new string[] { item });
                    else
                    {
                        if (listBox_local.Items.IndexOf(item) < 0)
                            listBox_local.Items.Add(item);
                    }
                }
                catch { }
            }
        }

        private void listBox_remote_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listBox_remote_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ListView.SelectedListViewItemCollection)))
            {
                var selects = (ListView.SelectedListViewItemCollection)e.Data.GetData(typeof(ListView.SelectedListViewItemCollection));
                if (SelectedRemoteFiles == null || SelectedRemoteFiles.Count() == 0)
                    SelectedRemoteFiles = selects.Cast<ListViewItem>().Select(x => (x.Tag as ItemInfo).info);
                else
                {
                    SelectedRemoteFiles = selects.Cast<ListViewItem>().Select(x => (x.Tag as ItemInfo).info).Concat(SelectedRemoteFiles);
                }
            }
        }

        private void button_clearLocal_Click(object sender, EventArgs e)
        {
            listBox_local.Items.Clear();
        }

        private void button_clearRemote_Click(object sender, EventArgs e)
        {
            SelectedRemoteFiles = null;
            listBox_remote.DataSource = null;
        }

        private void radioButton_MD5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_MD5.Checked)
            {
                checkBox_MD5.Checked = true;
                checkBox_MD5.Enabled = false;
            }
            else
            {
                checkBox_MD5.Enabled = true;
            }
        }
    }
}
