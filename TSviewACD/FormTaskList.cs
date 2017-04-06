using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public sealed partial class FormTaskList : Form
    {
        private FormTaskList()
        {
            InitializeComponent();
        }

        private static readonly FormTaskList _instance = new FormTaskList();

        public static FormTaskList Instance
        {
            get
            {
                return _instance;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            e.Cancel = true;
            Hide();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Start();
        }

        public void Start()
        {
            timer1.Enabled = true;
        }

        public void Stop()
        {
            timer1.Enabled = false;
            Hide();
            TSviewACD.FormClosing.Instance.Close();
        }

        int tic = 0;
        JobControler.Job[] internalJobList;


        private void timer1_Tick(object sender, EventArgs e)
        {
            tic++;
            if (tic > 10000) tic = 0;
            listView1.Invalidate();
            if (tic % 10 != 0) return;

            var joblist = JobControler.JobList();
            if (joblist == null) return;
            var now = DateTime.Now;
            internalJobList = joblist.Where(x => !x.IsHidden)
                .OrderBy(x => (x.IsInfo)? 0: (x.IsDone) ? 1 : (x.IsRunning) ? 2 : 3)
                .ThenBy(x => x.index)
                .ThenBy(x => (x.StartTime == default(DateTime)) ? now : x.StartTime)
                .ThenBy(x => x.QueueTime).ToArray();
            listView1.VirtualListSize = internalJobList.Length;
            if(listView1.VirtualListSize > 0)
            {
                var str = new StringBuilder();
                str.AppendFormat("All:{0} ", internalJobList.Length);
                int d = JobControler.JobTypeCount(JobControler.JobClass.Download);
                int u = JobControler.JobTypeCount(JobControler.JobClass.Upload);
                int p = JobControler.JobTypeCount(JobControler.JobClass.PlayDownload);
                int o = internalJobList.Length - (d + u + p);
                if (d > 0)
                    str.AppendFormat("Download:{0} ", d);
                if (u > 0)
                    str.AppendFormat("Upload:{0} ", u);
                if (p > 0)
                    str.AppendFormat("Play:{0} ", p);
                if (o > 0)
                    str.AppendFormat("Other:{0} ", o);
                label1.Text = str.ToString();
            }
            else
            {
                label1.Text = "";
            }
            if (internalJobList.Length == 0)
                Stop();
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        ListViewItem[] runingItems;
        int firstItem;

        private ListViewItem GetListViewItem(int index)
        {
            var jobs = internalJobList;
            if (jobs == null) return new ListViewItem();
            if (jobs.Length <= index) return new ListViewItem();
            var j = jobs[index];
            var item = new ListViewItem(j.DisplayName);
            item.Tag = j;
            return item;
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (runingItems != null && e.ItemIndex >= firstItem && e.ItemIndex < firstItem + runingItems.Length)
            {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                if (runingItems.All(x => !((x.Tag as JobControler.Job)?.IsDone ?? true)))
                {
                    e.Item = runingItems[e.ItemIndex - firstItem];
                    return;
                }
                runingItems = null;
            }
            //A cache miss, so create a new ListViewItem and pass it back.
            e.Item = GetListViewItem(e.ItemIndex);
        }

        private void listView1_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            //We've gotten a request to refresh the cache.
            //First check if it's really neccesary.
            if (runingItems != null && e.StartIndex >= firstItem && e.EndIndex <= firstItem + runingItems.Length)
            {
                //If the newly requested cache is a subset of the old cache, 
                //no need to rebuild everything, so do nothing.
                if (runingItems.All(x => !(x.Tag as JobControler.Job).IsDone))
                    return;
            }

            //Now we need to rebuild the cache.
            firstItem = e.StartIndex;
            int length = e.EndIndex - e.StartIndex + 1; //indexes are inclusive
            runingItems = new ListViewItem[length];

            //Fill the cache with the appropriate ListViewItems.
            for (int i = 0; i < length; i++)
            {
                runingItems[i] = GetListViewItem(i + firstItem);
            }
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            var job = e.Item.Tag as JobControler.Job;

            if (job == null) return;

            if (job.Progress < 0)
            {
                // Draw the background for an unselected item.
                using (var brush = new LinearGradientBrush(e.Bounds, Color.LightBlue, Color.LightBlue, LinearGradientMode.Horizontal))
                {
                    brush.InterpolationColors = new ColorBlend()
                    {
                        Colors = new Color[] { Color.LightBlue, Color.White, Color.LightBlue },
                        Positions = new float[] { 0, 1 / 2f, 1 }
                    };
                    brush.TranslateTransform(tic * 10, 0);
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }
            else if (job.Progress < 1)
            {
                var rect = new Rectangle(e.Bounds.Location, new Size((int)(e.Bounds.Width * job.Progress), e.Bounds.Height));
                if(job.IsInfo)
                    e.Graphics.FillRectangle(Brushes.Plum, rect);
                else
                    e.Graphics.FillRectangle(Brushes.LightBlue, rect);
            }
            else if (job.Progress >= 10)
            {
                var rect = new Rectangle(e.Bounds.Location, new Size((int)(e.Bounds.Width * (job.Progress - 10)), e.Bounds.Height));
                e.Graphics.FillRectangle(Brushes.LightYellow, rect);
            }
            else if (job.Progress > 1)
            {
                e.Graphics.FillRectangle(Brushes.LightYellow, e.Bounds);
            }
            else if (double.IsNaN(job.Progress))
            {
                e.Graphics.FillRectangle(Brushes.OrangeRed, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.LightGreen, e.Bounds);
            }

            if ((e.State & ListViewItemStates.Selected) != 0)
            {
                e.DrawFocusRectangle();
            }

            var rect1 = e.Bounds;
            rect1.Height /= 2;
            var rect2 = new Rectangle(rect1.X, rect1.Y + rect1.Height, rect1.Width, rect1.Height);

            e.Graphics.DrawString(
                        job.DisplayName,
                        e.Item.Font,
                        SystemBrushes.WindowText,
                        rect1);
            e.Graphics.DrawString(
                        job.ProgressStr,
                        e.Item.Font,
                        SystemBrushes.WindowText,
                        rect2);
        }

        private void FormTaskList_Load(object sender, EventArgs e)
        {
            listView1.Columns[0].Width = listView1.ClientSize.Width;
            var imlist = new ImageList();
            imlist.ImageSize = new Size(1, 35);
            listView1.SmallImageList = imlist;
            FixPosition();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            JobControler.CancelAllforUser();
            TSviewACD.FormClosing.Instance.Show();
        }

        private void listView1_ClientSizeChanged(object sender, EventArgs e)
        {
            listView1.BeginUpdate();
            listView1.Columns[0].Width = listView1.ClientSize.Width;
            listView1.EndUpdate();
        }

        bool manualchange = false;
        bool manuallocation = false;
        public void FixPosition()
        {
            if (manuallocation) return;

            if (Program.MainForm != null)
            {
                var s = Screen.FromControl(Program.MainForm);

                Point p = new Point(Program.MainForm.Left + Program.MainForm.Width, Program.MainForm.Top);
                Rectangle r = new Rectangle(p, Size);
                if (s.WorkingArea.Contains(r))
                {
                    Location = p;
                }
                else
                {
                    Point p2 = new Point(Program.MainForm.Left - Width, Program.MainForm.Top);
                    Rectangle r2 = new Rectangle(p2, Size);
                    if (s.WorkingArea.Contains(r2))
                    {
                        Location = p2;
                    }
                    else
                    {
                        Location = p;
                    }
                }
            }
        }

        private void FormTaskList_LocationChanged(object sender, EventArgs e)
        {
            if (manualchange) manuallocation = true;
        }

        private void FormTaskList_ResizeBegin(object sender, EventArgs e)
        {
            manualchange = true;
        }
    }

    public class ConsoleJobDisp
    {
        static System.Threading.Timer t;
        static public void Run()
        {
            if (ConsoleFunc.IsOutputRedirected) return;
            t = new System.Threading.Timer((o) =>
            {
                Console.Clear();
                var height = Console.WindowHeight;
                var width = Console.WindowWidth;
                var joblist = JobControler.JobList();
                var i = 0;
                foreach (var j in joblist.Where(x => !x.IsHidden).OrderBy(x => x.index))
                {
                    Console.SetCursorPosition(0, i++);
                    Console.Write(j.DisplayName);
                    if (i > height - 1) break;
                    Console.SetCursorPosition(4, i++);
                    Console.Write(j.ProgressStr);
                    if (i > height - 1) break;
                }
            }, null, 0, 1000);
        }
    }

    public class JobControler
    {
        public enum JobClass
        {
            Normal,
            Reload,
            WaitReload,
            WaitChanges,
            Download,
            Upload,
            Trash,
            Play,
            PlayDownload,
            Clean,
            ControlMaster,
            UploadInfo,
            DownloadInfo,
        }


        public class Job
        {
            public class SubInfo
            {
                public long index
                {
                    get; internal set;
                }
                public long size;
                public long pos
                {
                    set
                    {
                        if (value > 0)
                        {
                            if (type == SubType.UploadFile)
                            {
                                UploadProgress.AddOrUpdate(index, value, (key, val) => value);
                            }
                            else if (type == SubType.DownloadFile)
                            {
                                DownloadProgress.AddOrUpdate(index, value, (key, val) => value);
                            }
                        }
                    }
                }
                public enum SubType
                {
                    unknown,
                    UploadFile,
                    UploadDirectory,
                    DownloadFile,
                }
                public SubType type;
            }

            private string _ProgressStr;
            public string DisplayName;
            public string ProgressStr
            {
                get {
                    switch (JobType)
                    {
                        case JobClass.Upload:
                            return string.Format("Upload({0}/{1}) : {2}", index, UploadAll, _ProgressStr);
                        case JobClass.Download:
                            return string.Format("Download({0}/{1}) : {2}", index, DownloadAll, _ProgressStr);
                        case JobClass.UploadInfo:
                            {
                                var sb = new StringBuilder();
                                var subtotal = UploadProgress.Aggregate(0L, (acc, kvp) => acc + kvp.Value);
                                sb.Append("Upload ");
                                sb.AppendFormat("File({0}/{1}) ", UploadFileDone, UploadFileAll);
                                sb.AppendFormat("Folder({0}/{1}) ", UploadFolderDone, UploadFolderAll);
                                Progress = (double)(subtotal + UploadProgressDone) / UploadTotal;
                                if (double.IsNaN(Progress)) Progress = 1;
                                sb.AppendFormat("{0:#,0}/{1:#,0}({2:0.00%}) ", subtotal + UploadProgressDone, UploadTotal, Progress);
                                var speed = (subtotal + UploadProgressDone) / (DateTime.Now - StartTime).TotalSeconds;
                                var togo = Math.Round((UploadTotal - subtotal - UploadProgressDone) / speed);
                                togo = (double.IsInfinity(togo) || double.IsNaN(togo)) ? 0 : togo;
                                sb.AppendFormat("{0} [to go {1}]", ConvertUnit(speed), TimeSpan.FromSeconds(togo));
                                return sb.ToString();
                            }
                        case JobClass.DownloadInfo:
                            {
                                var sb = new StringBuilder();
                                var subtotal = DownloadProgress.Aggregate(0L, (acc, kvp) => acc + kvp.Value);
                                sb.Append("Download ");
                                sb.AppendFormat("({0}/{1}) ", DownloadDone, DownloadAll);
                                Progress = (double)(subtotal + DownloadProgressDone) / DownloadTotal;
                                if (double.IsNaN(Progress)) Progress = 1;
                                sb.AppendFormat("{0:#,0}/{1:#,0}({2:0.00%}) ", subtotal + DownloadProgressDone, DownloadTotal, Progress);
                                var speed = (subtotal + DownloadProgressDone) / (DateTime.Now - StartTime).TotalSeconds;
                                var togo = Math.Round((DownloadTotal - subtotal - DownloadProgressDone) / speed);
                                togo = (double.IsInfinity(togo) || double.IsNaN(togo)) ? 0 : togo;
                                sb.AppendFormat("{0} [to go {1}]", ConvertUnit(speed), TimeSpan.FromSeconds(togo));
                                return sb.ToString();
                            }
                        default:
                            return _ProgressStr;
                    }
                }
                set { _ProgressStr = value; }
            }
            public bool IsInfo
            {
                get { return (JobType == JobClass.UploadInfo || JobType == JobClass.DownloadInfo) ? true : false; }
            }
            public double Progress = 0;
            public object Result;
            public object[] ResultOfDepend;
            public long index;
            public JobClass JobType
            {
                get; internal set;
            }
            public SubInfo JobInfo
            {
                get; internal set;
            }
            private CancellationTokenSource cts = new CancellationTokenSource();
            internal ConcurrentQueue<Job> DependsOn = new ConcurrentQueue<Job>();
            internal Action<object> JobAction;
            internal Task JobTask;
            internal bool _delete = false;
            internal bool _isdeleted = false;
            public DateTime QueueTime
            {
                get; internal set;
            }
            public DateTime StartTime
            {
                get; internal set;
            }
            public DateTime FinishTime
            {
                get; internal set;
            }
            public bool IsError = false;
            public bool DoAlways = false;
            public bool WeekDepend = false;
            internal bool _done = false;
            internal ManualResetEvent _start = new ManualResetEvent(false);
            internal ManualResetEvent _run = new ManualResetEvent(false);

            public CancellationToken ct
            {
                get { return cts.Token; }
            }

            public void Cancel()
            {
                cts.Cancel();
                if (!IsRunning)
                {
                    _delete = true;
                    Task.Delay(5000).ContinueWith((task) => RemoveJob(this));
                    joberaser.Set();
                }
            }

            public void Wait(int timeout = -1, CancellationToken ct = default(CancellationToken))
            {
                if (cts.IsCancellationRequested) return;
                WaitHandle.WaitAny(new WaitHandle[] { _start, ct.WaitHandle });
                JobTask?.Wait(timeout, ct);
            }

            public Task WaitTask(int timeout = -1, CancellationToken ct = default(CancellationToken))
            {
                return Task.Run(() =>
                {
                    WaitHandle.WaitAny(new WaitHandle[] { _start, ct.WaitHandle });
                    JobTask?.Wait(timeout, ct);
                }, ct);
            }

            public bool IsDone
            {
                get {
                    return cts.Token.IsCancellationRequested
                        || (JobTask?.IsCanceled ?? false)
                        || (JobTask?.IsCompleted ?? false)
                        || (JobTask?.IsFaulted ?? false)
                        || _done;
                }
            }
            public bool IsCanceled
            {
                get
                {
                    return cts.Token.IsCancellationRequested
                        || ((DoAlways)? false: DependsOn?.Any(x => x.IsCanceled) ?? false)
                        || (JobTask?.IsCanceled ?? false);
                }
            }
            public bool IsRunning
            {
                get {
                    return JobTask?.Status == TaskStatus.Running
                      || JobTask?.Status == TaskStatus.WaitingToRun;
                }
            }
            public bool IsHidden
            {
                get
                {
                    return JobType == JobClass.WaitReload 
                        || JobType == JobClass.WaitChanges
                        || JobType == JobClass.Clean
                        || JobType == JobClass.ControlMaster;
                }
            }

            public void Error(string str)
            {
                Progress = double.NaN;
                IsError = true;
                ProgressStr = str;
            }
        }

        static ConcurrentBag<Job> joblist = new ConcurrentBag<Job>();
        static ConcurrentDictionary<JobClass, ConcurrentBag<Job>> joblist_type = new ConcurrentDictionary<JobClass, ConcurrentBag<Job>>();
        static BlockingCollection<Job> StartQueue = new BlockingCollection<Job>();
        static BlockingCollection<Task> EndQueue = new BlockingCollection<Task>();
        static bool jobempty = true;
        static AutoResetEvent joberaser = new AutoResetEvent(false);

        static SynchronizationContext synchronizationContext = SynchronizationContext.Current;

        public static bool IsReloading = false;
        public static long UploadAll = 0;
        public static long UploadCur = 0;
        public static long UploadFileAll = 0;
        public static long UploadFileDone = 0;
        public static long UploadFolderAll = 0;
        public static long UploadFolderDone = 0;
        public static long UploadTotal = 0;
        public static long DownloadAll = 0;
        public static long DownloadCur = 0;
        public static long DownloadDone = 0;
        public static long DownloadTotal = 0;
        public static ConcurrentDictionary<long, long> UploadProgress = new ConcurrentDictionary<long, long>();
        public static long UploadProgressDone = 0;
        public static ConcurrentDictionary<long, long> DownloadProgress = new ConcurrentDictionary<long, long>();
        public static long DownloadProgressDone = 0;

        static public bool IsEmpty
        {
            get { return joblist.IsEmpty && jobempty; }
        }

        static void TriggerDisplay()
        {
            synchronizationContext.Post((o) =>
            {
                if (!IsEmpty && !FormTaskList.Instance.Visible && joblist.Any(x => !x.IsHidden))
                {
                    FormTaskList.Instance.Show();
                    FormTaskList.Instance.BringToFront();
                }
            }, null);
        }

        public static void ErrorOut(string str)
        {
            synchronizationContext.Post((o) =>
            {
                if (!IsEmpty && !FormErrorLog.Instance.Visible && joblist.Any(x => !x.IsHidden))
                {
                    FormErrorLog.Instance.Show();
                    FormErrorLog.Instance.BringToFront();
                }
                FormErrorLog.Instance.ErrorLog(str);
            }, null);
        }

        public static void ErrorOut(string format, params object[] args)
        {
            ErrorOut(string.Format(format, args));
        }

        static Job UploadInfoJob;
        static Job DownloadInfoJob;

        static private Job InternalCreateJob(JobClass type, Job.SubInfo info)
        {
            var newjob = new Job();
            newjob.JobType = type;
            joblist.Add(newjob);
            jobempty = false;
            if (type == JobClass.Upload)
            {
                if(UploadInfoJob == null)
                {
                    UploadInfoJob = CreateNewJob(JobClass.UploadInfo);
                    UploadInfoJob.DisplayName = "Upload Progress";
                    UploadInfoJob.StartTime = DateTime.Now;
                }
                Interlocked.Increment(ref UploadAll);
                newjob.index = Interlocked.Increment(ref UploadCur);
                info.index = newjob.index;
                newjob.JobInfo = info;
                if (info?.type == Job.SubInfo.SubType.UploadFile)
                {
                    Interlocked.Increment(ref UploadFileAll);
                    Interlocked.Add(ref UploadTotal, info.size);
                }
                else if (info?.type == Job.SubInfo.SubType.UploadDirectory)
                {
                    Interlocked.Increment(ref UploadFolderAll);
                }
            }
            else if (type == JobClass.Download)
            {
                if (DownloadInfoJob == null)
                {
                    DownloadInfoJob = CreateNewJob(JobClass.DownloadInfo);
                    DownloadInfoJob.DisplayName = "Download Progress";
                    DownloadInfoJob.StartTime = DateTime.Now;
                }
                Interlocked.Increment(ref DownloadAll);
                newjob.index = Interlocked.Increment(ref DownloadCur);
                info.index = newjob.index;
                newjob.JobInfo = info;
                if (info?.type == Job.SubInfo.SubType.DownloadFile)
                {
                    Interlocked.Add(ref DownloadTotal, info.size);
                }
            }
            joblist_type.AddOrUpdate(type,
                (key) =>
                {
                    var newitem = new ConcurrentBag<Job>();
                    newitem.Add(newjob);
                    return newitem;
                },
                (key, value) =>
                {
                    value.Add(newjob);
                    return value;
                });
            return newjob;
        }

        static public Job CreateNewJob(JobClass type = JobClass.Normal, Job.SubInfo info = null)
        {
            var newjob = InternalCreateJob(type, info);
            TriggerDisplay();
            return newjob;
        }

        static public Job CreateNewJob(JobClass type = JobClass.Normal, Job.SubInfo info = null, params Job[] depends)
        {
            var newjob = InternalCreateJob(type, info);
            foreach (var d in depends)
            {
                if(d != null)
                    newjob.DependsOn.Enqueue(d);
            }
            TriggerDisplay();
            return newjob;
        }

        static public void Run(Job target, Action<object> JobAction)
        {
            target.JobAction = JobAction;
            StartQueue.Add(target);
        }

        static private bool CancelCheck(Job j)
        {
            if (!j.DependsOn.IsEmpty && j.DependsOn.Any(x => x.IsCanceled || x.IsError))
            {
                if (!j.DoAlways)
                {
                    j.Cancel();
                    return true;
                }
            }
            return false;
        }

        static private void DeleteJob()
        {
            foreach (var key in joblist_type.Keys)
            {
                if (joblist_type[key].All(x => x._isdeleted))
                {
                    ConcurrentBag<Job> prevval;
                    if (joblist_type.TryRemove(key, out prevval))
                    {
                        if (prevval.Any(x => !x._isdeleted))
                        {
                            foreach (var reJob in prevval.Where(x => !x._isdeleted))
                            {
                                joblist_type.AddOrUpdate(key,
                                    (key1) =>
                                    {
                                        var newitem = new ConcurrentBag<Job>();
                                        newitem.Add(reJob);
                                        return newitem;
                                    },
                                    (key1, value) =>
                                    {
                                        value.Add(reJob);
                                        return value;
                                    });
                            }
                        }
                    }
                }
            }
            var oldjoblist = joblist;
            if (oldjoblist.All(x => x._isdeleted))
            {
                joblist = new ConcurrentBag<Job>();
                foreach (var j in oldjoblist.Where(x => !x._isdeleted))
                    joblist.Add(j);
            }
            if (joblist.IsEmpty)
            {
                Interlocked.Exchange(ref UploadAll, 0);
                Interlocked.Exchange(ref UploadCur, 0);
                Interlocked.Exchange(ref UploadFileAll, 0);
                Interlocked.Exchange(ref UploadFileDone, 0);
                Interlocked.Exchange(ref UploadFolderAll, 0);
                Interlocked.Exchange(ref UploadFolderDone, 0);
                Interlocked.Exchange(ref UploadTotal, 0);
                Interlocked.Exchange(ref DownloadAll, 0);
                Interlocked.Exchange(ref DownloadCur, 0);
                Interlocked.Exchange(ref DownloadDone, 0);
                Interlocked.Exchange(ref DownloadTotal, 0);
                UploadProgress.Clear();
                Interlocked.Exchange(ref UploadProgressDone, 0);
                DownloadProgress.Clear();
                Interlocked.Exchange(ref DownloadProgressDone, 0);
                jobempty = true;
            }
        }

        static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        static readonly JobClass[] singlejob =
            {
                JobClass.Reload,
                JobClass.Trash,
                JobClass.Play,
                JobClass.PlayDownload,
            };

        static void JobStartCheck()
        {
            Parallel.ForEach(joblist_type.Keys, (jobtype) =>
            {
                var running_count = 0;
                var max_running = int.MaxValue;

                if (jobtype == JobClass.Download)
                {
                    running_count = joblist_type[jobtype].Where(x => x.IsRunning).Count();
                    max_running = Config.ParallelDownload;
                }
                else if (jobtype == JobClass.Upload)
                {
                    running_count = joblist_type[jobtype].Where(x => x.IsRunning).Count();
                    max_running = Config.ParallelUpload;
                }
                else if (singlejob.Contains(jobtype))
                {
                    running_count = joblist_type[jobtype].Where(x => x.IsRunning).Count();
                    max_running = 1;
                }

                foreach (var j in joblist_type[jobtype].Where(x => !x._delete).OrderBy(x => x.index))
                {
                    if (j.JobTask != null)
                    {
                        var canceled = CancelCheck(j);
                        if (!j.IsDone && !j.IsRunning)
                        {
                            if (!canceled && (j.DependsOn.IsEmpty || j.WeekDepend || j.DependsOn.All(x => x._delete || x.IsDone)))
                            {
                                j.ResultOfDepend = j.DependsOn.Select(x => x.Result).ToArray();

                                if (running_count < max_running)
                                {
                                    j.StartTime = DateTime.Now;
                                    j.JobTask.Start();
                                    running_count++;
                                }
                                if (running_count >= max_running)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            });
            DeleteJob();
        }
           

        static JobControler()
        {
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        var startjob = StartQueue.Take();
                        if (startjob.JobAction == null)
                        {
                            startjob._done = true;
                            RemoveJob(startjob);
                            continue;
                        }
                        if (startjob.ct.IsCancellationRequested)
                        {
                            startjob._done = true;
                            Task.Delay(5000).ContinueWith((t) => RemoveJob(startjob));
                            continue;
                        }
                        if(startjob.JobType == JobClass.Reload)
                        {
                            foreach (var i in joblist.Where(x => x != startjob && !x.IsRunning).Where(x => x.JobType == JobClass.Reload))
                                i.Cancel();
                        }
                        if (startjob.JobType == JobClass.Play)
                        {
                            foreach (var i in joblist.Where(x => x != startjob).Where(x => x.JobType == JobClass.Play))
                                i.Cancel();
                            foreach (var i in joblist.Where(x => x.JobType == JobClass.PlayDownload).Where(x => !x.DependsOn.Contains(startjob)))
                                i.Cancel();
                        }
                        startjob.QueueTime = DateTime.Now;
                        startjob.JobTask = new Task(startjob.JobAction, startjob, startjob.ct);
                        EndQueue.Add(startjob.JobTask.ContinueWith((t, target) =>
                        {
                            var s = target as Job;
                            s._done = true;
                            s.FinishTime = DateTime.Now;
                            if(s.JobInfo != null)
                            {
                                long val;
                                switch (s.JobInfo.type)
                                {
                                    case Job.SubInfo.SubType.UploadFile:
                                        while (!UploadProgress.TryRemove(s.index, out val))
                                            if(!UploadProgress.TryGetValue(s.index, out val))
                                                break;
                                        Interlocked.Add(ref UploadProgressDone, s.JobInfo.size);
                                        Interlocked.Increment(ref UploadFileDone);
                                        break;
                                    case Job.SubInfo.SubType.UploadDirectory:
                                        Interlocked.Increment(ref UploadFolderDone);
                                        break;
                                    case Job.SubInfo.SubType.DownloadFile:
                                        while (!DownloadProgress.TryRemove(s.index, out val))
                                            if (!DownloadProgress.TryGetValue(s.index, out val))
                                                break;
                                        Interlocked.Add(ref DownloadProgressDone, s.JobInfo.size);
                                        Interlocked.Increment(ref DownloadDone);
                                        break;
                                }
                            }
                            if(UploadInfoJob != null)
                            {
                                if(UploadFileDone + UploadFolderDone >= UploadAll)
                                {
                                    UploadInfoJob._done = true;
                                    UploadInfoJob._delete = true;
                                    var u = UploadInfoJob;
                                    Task.Delay(5000).ContinueWith((t2) =>
                                    {
                                        RemoveJob(u);
                                    });
                                    UploadInfoJob = null;
                                }
                            }
                            if (DownloadInfoJob != null)
                            {
                                if (DownloadDone >= DownloadAll)
                                {
                                    DownloadInfoJob._done = true;
                                    DownloadInfoJob._delete = true;
                                    var d = DownloadInfoJob;
                                    Task.Delay(5000).ContinueWith((t2) =>
                                    {
                                        RemoveJob(d);
                                    });
                                    DownloadInfoJob = null;
                                }
                            }
                            if (t.IsCanceled)
                            {
                                s.ProgressStr = "Operation canceled.";
                                s.Progress += 10;
                            }
                            if (t.IsFaulted)
                            {
                                var e = t.Exception;
                                e.Flatten().Handle(ex =>
                                {
                                    if (ex is OperationCanceledException)
                                    {
                                        s.ProgressStr = "Operation canceled.";
                                        s.Progress += 10;
                                    }
                                    else
                                    {
                                        Config.Log.LogOut(string.Format("TaskError : ERROR {0}", ex.ToString()));
                                        ErrorOut("TaskError : ERROR {0}", ex.ToString());
                                        s.ProgressStr = ex.Message;
                                        s.Progress = double.NaN;
                                    }
                                    return true;
                                });
                                e.Handle(ex =>
                                {
                                    return true;
                                });
                            }
                            s._delete = true;
                            Task.Delay(5000).ContinueWith((task) => RemoveJob(s));
                            joberaser.Set();
                        }, startjob));
                        joberaser.Set();
                        startjob._start.Set();
                    }
                }
                catch { }
            });
            Task.Run(() => {
                try
                {
                    while (true)
                    {
                        joberaser.WaitOne();
                        JobStartCheck();
                    }
                }
                catch { }
            });
            Task.Run(() => {
                while (true)
                {
                    var t = EndQueue.Take();
                    try
                    {
                        t.Wait();
                    }
                    catch { }
                }
            });
        }

        static public void RemoveJob(Job doneJob)
        {
            doneJob._isdeleted = true;
            joberaser.Set();
        }

        static public bool CancelAll()
        {
            lock (joblist)
            {
                foreach (var j in joblist.Where(x => !x._delete))
                {
                    j.Cancel();
                }
            }
            return !IsEmpty;
        }

        static public bool CancelAllforUser()
        {
            lock (joblist)
            {
                foreach (var j in joblist.Where(x => !x._delete).Where(x => x.JobType != JobClass.WaitChanges))
                {
                    j.Cancel();
                }
            }
            return !IsEmpty;
        }

        static public void CancelPlay()
        {
            lock (joblist)
            {
                foreach (var j in joblist.Where(x => !x._delete).Where(x => x.JobType == JobClass.Play || x.JobType == JobClass.PlayDownload))
                    j.Cancel();
            }
        }

        static public Job[] JobList()
        {
            var list = joblist.Where(x => !x._isdeleted).ToArray();
            return list;
        }

        static public int JobTypeCount(JobClass type)
        {
            ConcurrentBag<Job> ret;
            if (joblist_type.TryGetValue(type, out ret))
            {
                return ret.Where(x => !x._isdeleted).Count();
            }
            else
            {
                return 0;
            }
        }

        static private string ConvertUnit(double rate)
        {
            if (rate < 1024)
                return string.Format("{0:#,0.00}Byte/s", rate);
            if (rate < 1024 * 1024)
                return string.Format("{0:#,0.00}KiB/s", rate / 1024);
            if (rate < 1024 * 1024 * 1024)
                return string.Format("{0:#,0.00}MiB/s", rate / 1024 / 1024);
            if (rate < (double)1024 * 1024 * 1024 * 1024)
                return string.Format("{0:#,0.00}GiB/s", rate / 1024 / 1024 / 1024);
            if (rate < (double)1024 * 1024 * 1024 * 1024 * 1024)
                return string.Format("{0:#,0.00}TiB/s", rate / 1024 / 1024 / 1024 / 1024);
            return string.Format("{0:#,0.00}PiB/s", rate / 1024 / 1024 / 1024 / 1024);
        }
    }
}
