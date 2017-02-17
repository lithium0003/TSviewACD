using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

            var task = TaskCanceler.CreateTask("test download");
            cts = task.cts;
            try
            {
                button_Start.Enabled = false;
                var synchronizationContext = SynchronizationContext.Current;

                await SelectedRemoteFiles.ForEachAsync(async item =>
                {
                    await Task.Run(() =>
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
                            try
                            {
                                long length = item.contentProperties.size ?? 0;
                                using (var ret = new AmazonDriveBaseStream(DriveData.Drive, item, autodecrypt: false, ct: cts.Token))
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
                                    f.CopyTo(new NullStream());
                                }
                                synchronizationContext.Post(
                                (o) =>
                                {
                                    if (cts == null || cts.Token.IsCancellationRequested) return;
                                    var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                    listitem.SubItems[1].Text = "Done";
                                }, null);
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
                    }, cts.Token).ConfigureAwait(false);
                }, 5, cts.Token, false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                TaskCanceler.FinishTask(task);
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
