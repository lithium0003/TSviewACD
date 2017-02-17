using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormTestDownload : Form
    {
        public FormTestDownload()
        {
            InitializeComponent();
        }

        CancellationTokenSource cts;

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
                }
                else
                {
                    _SelectedRemoteFiles = value.ToArray()
                        .Select(x => DriveData.GetAllChildrenfromId(x.id))
                        .SelectMany(x => x.Select(y => y))
                        .Distinct()
                        .Where(x => x.kind != "ASSET")
                        .Where(x => x.kind != "FOLDER");
                    listView1.Items.AddRange(SelectedRemoteFiles.Select(x => {
                        var item = new ListViewItem(new string[]{ DriveData.GetFullPathfromId(x.id), ""});
                        item.Name = x.id;
                        return item;
                        }).ToArray());
                }
            }
        }

        private async void button_Start_Click(object sender, EventArgs e)
        {
            if (SelectedRemoteFiles == null) return;

            var task = Program.MainForm.CreateTask("test download");
            cts = task.cts;
            try
            {
                button_Start.Enabled = false;
                var synchronizationContext = SynchronizationContext.Current;

                await Task.Run(async () =>
                {
                    await SelectedRemoteFiles.ForEachAsync(async item =>
                    {
                        synchronizationContext.Post(
                            (o) =>
                            {
                                if (cts == null || cts.Token.IsCancellationRequested) return;
                                var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                listitem.SubItems[1].Text = "download...";
                            }, null);

                        cts.Token.ThrowIfCancellationRequested();
                        var retry = 5;
                        while (--retry > 0)
                        {
                            Func<string, Task> dodownload = async (hash) =>
                            {
                                long length = item.contentProperties.size ?? 0;
                                const int bufferlen = 16 * 1024 * 1024;
                                using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                                using (var ret = await DriveData.Drive.downloadFile(item, autodecrypt: false, ct: cts.Token))
                                using (var f = new PositionStream(ret, length))
                                {
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        synchronizationContext.Post(
                                            (o) =>
                                            {
                                                if (cts == null || cts.Token.IsCancellationRequested) return;
                                                var eo = o as PositionChangeEventArgs;
                                                var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                                listitem.SubItems[1].Text = eo.Log;
                                            }, evnt);
                                    };
                                    byte[] buffer = new byte[(length > bufferlen) ? bufferlen : length];
                                    while (length > 0)
                                    {
                                        cts.Token.ThrowIfCancellationRequested();
                                        var readbytes = await f.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                                        md5calc.TransformBlock(buffer, 0, readbytes, buffer, 0);
                                        length -= readbytes;
                                        if (buffer.Length > length)
                                            buffer = new byte[length];
                                    }
                                    md5calc.TransformFinalBlock(buffer, 0, 0);
                                    var MD5 = BitConverter.ToString(md5calc.Hash).ToLower().Replace("-", "");
                                    if (MD5 == hash)
                                    {
                                        synchronizationContext.Post(
                                            (o) =>
                                            {
                                                if (cts == null || cts.Token.IsCancellationRequested) return;
                                                var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                                listitem.SubItems[1].Text = "OK";
                                            }, null);
                                    }
                                    else
                                    {
                                        synchronizationContext.Post(
                                            (o) =>
                                            {
                                                if (cts == null || cts.Token.IsCancellationRequested) return;
                                                var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                                listitem.SubItems[1].Text = "*NG* MD5 remote:" + hash + " download:" + MD5;
                                            }, null);
                                    }
                                    return;
                                }
                            };

                            try
                            {
                                if (item.contentProperties.size > ConfigAPI.FilenameChangeTrickSize)
                                {
                                    Config.Log.LogOut("Download : <BIG FILE> temporary filename change");
                                    try
                                    {
                                        var tmpfile = await DriveData.Drive.renameItem(item.id, ConfigAPI.temporaryFilename + item.id);
                                        await dodownload(item.contentProperties.md5);
                                    }
                                    finally
                                    {
                                        await DriveData.Drive.renameItem(item.id, item.name);
                                    }
                                }
                                else
                                {
                                    await dodownload(item.contentProperties.md5);
                                }
                                break;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Config.Log.LogOut("Download : Error " + ex.Message);
                                continue;
                            }
                        }
                        if (retry == 0)
                        {
                            // failed
                            synchronizationContext.Post(
                                (o) =>
                                {
                                    if (cts == null || cts.Token.IsCancellationRequested) return;
                                    var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                    listitem.SubItems[1].Text = "Failed";
                                }, null);
                        }
                    }, 5, cts.Token, false);
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                Program.MainForm.FinishTask(task);
                cts = null;
                button_Start.Enabled = true;
            }
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FormTestDownload_FormClosing(object sender, FormClosingEventArgs e)
        {
            cts?.Cancel();
        }
    }

    //ref http://neue.cc/2014/03/14_448.html
    public static class EnumerableExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int concurrency, CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (action == null) throw new ArgumentNullException("action");
            if (concurrency <= 0) throw new ArgumentOutOfRangeException("concurrencyは1以上の必要があります");

            using (var semaphore = new SemaphoreSlim(initialCount: concurrency, maxCount: concurrency))
            {
                var exceptionCount = 0;
                var tasks = new List<Task>();

                foreach (var item in source)
                {
                    if (exceptionCount > 0) break;
                    cancellationToken.ThrowIfCancellationRequested();

                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(configureAwait);
                    var task = action(item).ContinueWith(t =>
                    {
                        semaphore.Release();

                        if (t.IsFaulted)
                        {
                            Interlocked.Increment(ref exceptionCount);
                            throw t.Exception;
                        }
                    });
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(configureAwait);
            }
        }
    }

}
