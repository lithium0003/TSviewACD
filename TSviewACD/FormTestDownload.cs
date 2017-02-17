using System;
using System.Collections.Concurrent;
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

        JobControler.Job runningjob;

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

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (SelectedRemoteFiles == null) return;

            button_Start.Enabled = false;
            var synchronizationContext = SynchronizationContext.Current;

            var masterjob = JobControler.CreateNewJob(JobControler.JobClass.Normal);
            masterjob.DisplayName = "Download test";
            masterjob.ProgressStr = "wait for download";
            runningjob = masterjob;
            var joblist = new ConcurrentBag<JobControler.Job>();
            Parallel.ForEach(SelectedRemoteFiles, (item) =>
            {
                var job = JobControler.CreateNewJob(JobControler.JobClass.Download, depends: masterjob);
                job.WeekDepend = true;
                job.DisplayName = DriveData.GetFullPathfromId(item.id);
                job.ProgressStr = "Wait for download";
                joblist.Add(job);
                var ct = CancellationTokenSource.CreateLinkedTokenSource(masterjob.ct, job.ct).Token;
                JobControler.Run(job, (j) =>
                {
                    synchronizationContext.Post(
                        (o) =>
                        {
                            if (ct.IsCancellationRequested) return;
                            var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                            listitem.SubItems[1].Text = "download...";
                        }, null);
                    (j as JobControler.Job).ProgressStr = "download...";

                    var retry = 5;
                    while (--retry > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            long length = item.contentProperties.size ?? 0;
                            using (var ret = new AmazonDriveBaseStream(DriveData.Drive, item, autodecrypt: false, parentJob: (j as JobControler.Job)))
                            using (var f = new PositionStream(ret, length))
                            {
                                f.PosChangeEvent += (src, evnt) =>
                                {
                                    synchronizationContext.Post(
                                        (o) =>
                                        {
                                            if (ct.IsCancellationRequested) return;
                                            var eo = o as PositionChangeEventArgs;
                                            var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                            listitem.SubItems[1].Text = eo.Log;
                                        }, evnt);
                                    (j as JobControler.Job).ProgressStr = evnt.Log;
                                    (j as JobControler.Job).Progress = (double)evnt.Position / evnt.Length;
                                };
                                f.CopyTo(new NullStream());
                            }
                            synchronizationContext.Post(
                            (o) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                                listitem.SubItems[1].Text = "Done";
                            }, null);
                            (j as JobControler.Job).ProgressStr = "Done.";
                            (j as JobControler.Job).Progress = 1;
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
                            if (ct.IsCancellationRequested) return;
                            var listitem = listView1.Items.Find(item.id, false).FirstOrDefault();
                            listitem.SubItems[1].Text = "Failed";
                        }, null);
                        (j as JobControler.Job).ProgressStr = "Failed";
                        (j as JobControler.Job).Progress = double.NaN;
                        (j as JobControler.Job).IsError = true;
                    }
                });
            });
            JobControler.Run(masterjob, (j) =>
            {
                masterjob.ProgressStr = "running...";
                masterjob.Progress = -1;
                var ct = (j as JobControler.Job).ct;
                Task.WaitAll(joblist.Select(x => x.WaitTask(ct: ct)).ToArray());
                masterjob.Progress = 1;
                masterjob.ProgressStr = "done.";
            });
            var afterjob = JobControler.CreateNewJob(JobControler.JobClass.Clean, depends: masterjob);
            afterjob.DisplayName = "clean up";
            afterjob.DoAlways = true;
            JobControler.Run(afterjob, (j) =>
            {
                afterjob.ProgressStr = "done.";
                afterjob.Progress = 1;
                runningjob = null;
                synchronizationContext.Post((o) =>
                {
                    button_Start.Enabled = true;
                }, null);
            });
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FormTestDownload_FormClosing(object sender, FormClosingEventArgs e)
        {
            runningjob?.Cancel();
        }
    }
}
