using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace TSviewACD
{
    class ConsoleFunc
    {
        private static AmazonDrive Drive = DriveData.Drive;

        private static Dictionary<string, ItemInfo> DriveTree = new Dictionary<string, ItemInfo>();
        private static List<Changes_Info> treedata = new List<Changes_Info>();

        public static bool IsOutputRedirected = false;

        public static int MainFunc(string[] args)
        {
            bool inputRedirected = IsRedirected(GetStdHandle(StandardHandle.Input));
            if (inputRedirected)
            {
                Config.MasterPassword = Console.ReadLine();
            }

            bool outputRedirected = IsRedirected(GetStdHandle(StandardHandle.Output));
            Stream initialOut = null;
            if (outputRedirected)
            {
                initialOut = Console.OpenStandardOutput();
                IsOutputRedirected = true;
            }

            bool errorRedirected = IsRedirected(GetStdHandle(StandardHandle.Error));
            Stream initialError = null;
            if (errorRedirected)
            {
                initialError = Console.OpenStandardError();
            }

            if (!AttachConsole(-1))
                AllocConsole();

            int codepage = GetConsoleOutputCP();
            if (outputRedirected)
            {
                Console.SetOut(new StreamWriter(initialOut, Encoding.GetEncoding(codepage)));
            }
            else
            {
                Console.OutputEncoding = Encoding.GetEncoding(codepage);
            }

            if (errorRedirected)
            {
                Console.SetError(new StreamWriter(initialError, Encoding.GetEncoding(codepage)));
            }
            else
            {
                SetStdHandle(StandardHandle.Error, GetStdHandle(StandardHandle.Output));
            }

            Console.Error.WriteLine("");

            Console.CancelKeyPress += new ConsoleCancelEventHandler(CtrlC_Handler);

            var paramArgsList = new List<string>();
            var targetArgsList = new List<string>();
            bool skipparam = false;
            foreach (var p in args)
            {
                if (skipparam) targetArgsList.Add(p);
                else
                {
                    if (p == "-")
                    {
                        skipparam = true;
                        continue;
                    }
                    if (p.StartsWith("--")) paramArgsList.Add(p.Substring(2));
                    else targetArgsList.Add(p);
                }
            }
            if (targetArgsList.Count == 0)
                targetArgsList.Add("help");

            var paramArgs = paramArgsList.ToArray();
            var targetArgs = targetArgsList.ToArray();

            foreach (var p in paramArgs)
            {
                switch (p)
                {
                    case "debug":
                        Console.Error.WriteLine("(--debug: debug output mode)");
                        Config.debug = true;
                        break;
                }
            }

            switch (targetArgs[0])
            {
                case "help":
                    Console.WriteLine("usage");
                    Console.WriteLine("\thelp                                      : show help");
                    Console.WriteLine("\tlist (REMOTE_PATH)                        : list item");
                    Console.WriteLine("\t\t--recursive: recursive mode");
                    Console.WriteLine("\t\t--md5: show MD5 hash");
                    Console.WriteLine("\t\t--nodecrypt: disable auto decrypt");
                    Console.WriteLine("");
                    Console.WriteLine("\tdownload (REMOTE_PATH) (LOCAL_DIR_PATH)");
                    Console.WriteLine("\tdownload (REMOTE_PATH) (LOCAL_DIR_PATH) (IGNORE_LIST)");
                    Console.WriteLine("\t : download item(s)");
                    Console.WriteLine("\tdownload_index (INDEX_PATH) (REMOTE_PATH) (LOCAL_DIR_PATH)");
                    Console.WriteLine("\tdownload_index (INDEX_PATH) (REMOTE_PATH) (LOCAL_DIR_PATH) (IGNORE_LIST)");
                    Console.WriteLine("\t : make link index after download item(s)");
                    Console.WriteLine("\t\t--nodecrypt: disable auto decrypt");
                    Console.WriteLine("");
                    Console.WriteLine("\tupload   (LOCAL_FILE_PATH) (REMOTE_PATH)  : upload item");
                    Console.WriteLine("\tupload_watch (INDEX_PATH) (LOCAL_PATH_BASE) (REMOTE_PATH)");
                    Console.WriteLine("\t : watch INDEX_PATH for index file(file location file).");
                    Console.WriteLine("\t : upload directed files and remove local file");
                    Console.WriteLine("\t\t--md5 : hash check for conflict");
                    Console.WriteLine("\t\t--createpath: make upload target folder mode");
                    Console.WriteLine("\t\t--crypt1: crypt upload mode(CTR mode)");
                    Console.WriteLine("\t\t--crypt1name: crypt filename(CTR mode)");
                    Console.WriteLine("\t\t--crypt2: crypt upload mode(CBC mode CarrotDAV)");
                    Console.WriteLine("\t\t--nocrypt: nomal upload mode");
                    Console.WriteLine("\t\t--nodecrypt: disable auto decrypt");
                    Console.WriteLine("");
                    Console.WriteLine("\t\t--debug : debug log output");
                    break;
                case "list":
                    CheckMasterPassword();
                    Console.Error.WriteLine("list...");
                    return ListItems(targetArgs, paramArgs);
                case "download":
                    CheckMasterPassword();
                    Console.Error.WriteLine("download...");
                    return Download(targetArgs, paramArgs);
                case "download_index":
                    CheckMasterPassword();
                    Console.Error.WriteLine("download with index...");
                    return Download(targetArgs, paramArgs, true);
                case "upload":
                    CheckMasterPassword();
                    Console.Error.WriteLine("upload...");
                    return Upload(targetArgs, paramArgs);
                case "upload_watch":
                    CheckMasterPassword();
                    Console.Error.WriteLine("upload watch...");
                    return Upload(targetArgs, paramArgs, true);
            }
            return 0;
        }

        static void CheckMasterPassword()
        {
            if (!Config.IsMasterPasswordCorrect)
            {
                Thread t = new Thread(new ThreadStart(() =>
                {
                    using(var f = new FormMasterPass())
                    {
                        f.ShowDialog();
                    }
                }));
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
                t.Join();

                if (!Config.IsMasterPasswordCorrect)
                {
                    Console.Error.Write("Master Password Incorrect.");
                    Environment.Exit(1);
                }
            }
        }

        async protected static void CtrlC_Handler(object sender, ConsoleCancelEventArgs args)
        {
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Cancel...");
            JobControler.CancelAll();
            args.Cancel = true;
            await Task.Run(() =>
            {
                while (!JobControler.IsEmpty)
                    Thread.Sleep(100);
            }).ConfigureAwait(false);
        }


        private static JobControler.Job Login()
        {
            var job = JobControler.CreateNewJob();
            job.DisplayName = "login";
            var ct = job.ct;
            JobControler.Run(job, (j) =>
            {
                var initialized = false;
                Console.Error.WriteLine("Login Start.");
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
                    Console.Error.WriteLine("Login done.");
                }
                else
                {
                    Console.Error.WriteLine("Login failed.");
                    throw new ApplicationException("Login failed.");
                }
            });
            return job;
        }


        static FileMetadata_Info[] FindItems(string[] path_str, bool recursive = false, FileMetadata_Info root = null)
        {
            List<FileMetadata_Info> ret = new List<FileMetadata_Info>();
            if (root == null)
            {
                root = DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].info;
            }
            if (!(path_str?.Length > 0))
            {
                ret.Add(root);
                return ret.ToArray();
            }
            while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
            {
                path_str = path_str.Skip(1).ToArray();
            }
            if (path_str.Length == 0)
            {
                ret.Add(root);
                return ret.ToArray();
            }

            var children = DriveData.AmazonDriveTree[root.id].children.Select(x => x.Value);

            foreach (var c in children)
            {
                if (c.DisplayName == path_str[0]
                    ||
                    ((path_str[0].Contains('*') || path_str[0].Contains('?'))
                            && Regex.IsMatch(c.DisplayName, Regex.Escape(path_str[0]).Replace("\\*", ".*").Replace("\\?", "."))))
                {
                    if (c.info.kind == "FOLDER")
                    {
                        ret.AddRange(FindItems((recursive && path_str[0] == "*") ? path_str : path_str.Skip(1).ToArray(), recursive, c.info));
                    }
                    else
                    {
                        if (path_str[0] == c.DisplayName
                            ||
                            (((path_str[0].Contains('*') || path_str[0].Contains('?'))
                                && Regex.IsMatch(c.DisplayName, Regex.Escape(path_str[0]).Replace("\\*", ".*").Replace("\\?", ".")))))
                        {
                            ret.Add(c.info);
                        }
                    }
                }
            }
            if (recursive)
            {
                ret.Sort((x, y) => (DriveData.GetFullPathfromId(x.id)).CompareTo(DriveData.GetFullPathfromId(y.id)));
                return ret.ToArray();
            }
            else
            {
                ret.Sort((x, y) => x.name.CompareTo(y.name));
                return ret.ToArray();
            }
        }


        static string FindItemsID(string[] path_str, FileMetadata_Info root = null)
        {
            if (path_str.Length == 0)
            {
                return root?.id;
            }
            while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
            {
                path_str = path_str.Skip(1).ToArray();
            }
            if (path_str.Length == 0)
            {
                return root?.id;
            }

            if (root == null)
            {
                root = DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].info;
            }

            var children = DriveData.AmazonDriveTree[root.id].children.Select(x => x.Value);

            foreach (var c in children)
            {
                if (c.DisplayName == path_str[0])
                {
                    if (c.info.kind == "FOLDER")
                    {
                        return FindItemsID(path_str.Skip(1).ToArray(), c.info);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        static int ListItems(string[] targetArgs, string[] paramArgs)
        {
            var job = JobControler.CreateNewJob(JobControler.JobClass.ControlMaster);
            job.DisplayName = "ListItem";
            JobControler.Run(job, (j) =>
            {
                string remotepath = null;
                FileMetadata_Info[] target = null;

                if (targetArgs.Length > 1)
                {
                    remotepath = targetArgs[1];
                    remotepath = remotepath.Replace('\\', '/');
                }

                bool recursive = false;
                bool showmd5 = false;
                bool nodecrypt = false;
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "recursive":
                            Console.Error.WriteLine("(--recursive: recursive mode)");
                            recursive = true;
                            break;
                        case "md5":
                            Console.Error.WriteLine("(--md5: show MD5 hash)");
                            showmd5 = true;
                            break;
                        case "nodecrypt":
                            Console.Error.WriteLine("(--nodecrypt: disable auto decrypt)");
                            nodecrypt = true;
                            break;
                    }
                }

                try
                {
                    var loginjob = Login();
                    var initjob = AmazonDriveControl.InitAlltree(loginjob);
                    initjob.Wait(ct: job.ct);
                    target = FindItems(remotepath?.Split('/'), recursive: recursive);

                    if (target.Length < 1)
                    {
                        job.Result = 2;
                        return;
                    }

                    Console.Error.WriteLine("Found : " + target.Length);
                    foreach (var item in target)
                    {
                        string detail = "";
                        if (showmd5) detail = "\t" + item.contentProperties?.md5;

                        if (recursive)
                            Console.WriteLine(DriveData.GetFullPathfromId(item.id, nodecrypt) + detail);
                        else
                            Console.WriteLine(((nodecrypt) ? item.name : DriveData.AmazonDriveTree[item.id].DisplayName) + ((item.kind == "FOLDER") ? "/" : "") + detail);
                    }

                    job.Result = 0;
                }
                catch (OperationCanceledException)
                {
                    job.Result = -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    job.Result = 1;
                }
            });
            try
            {
                job.Wait(ct: job.ct);
            }
            catch (OperationCanceledException)
            {
            }
            Config.IsClosing = true;
            Console.Out.Flush();
            return (job.Result as int?) ?? -1;
        }

        static int Download(string[] targetArgs, string[] paramArgs, bool index_mode = false)
        {
            var masterjob = JobControler.CreateNewJob(JobControler.JobClass.ControlMaster);
            masterjob.DisplayName = "Download";
            var ct = masterjob.ct;
            JobControler.Run(masterjob, (j) =>
            {
                string remotepath = null;
                string localpath = null;
                string indexpath = null;
                string ignorespath = null;
                FileMetadata_Info[] target = null;

                if (index_mode)
                {
                    if (targetArgs.Length > 4)
                        ignorespath = targetArgs[4];
                    if (targetArgs.Length > 3)
                        localpath = targetArgs[3];
                    if (targetArgs.Length > 2)
                    {
                        remotepath = targetArgs[2];
                        remotepath = remotepath.Replace('\\', '/');
                    }
                    if (targetArgs.Length > 1)
                        indexpath = targetArgs[1];
                }
                else
                {
                    if (targetArgs.Length > 3)
                        ignorespath = targetArgs[3];
                    if (targetArgs.Length > 2)
                        localpath = targetArgs[2];
                    if (targetArgs.Length > 1)
                    {
                        remotepath = targetArgs[1];
                        remotepath = remotepath.Replace('\\', '/');
                    }
                }

                if (string.IsNullOrEmpty(remotepath))
                {
                    masterjob.Result = 0;
                    return;
                }

                bool autodecrypt = true;
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "nodecrypt":
                            Console.Error.WriteLine("(--nodecrypt: disable auto decrypt)");
                            autodecrypt = false;
                            break;
                    }
                }
                AmazonDriveControl.autodecrypt = autodecrypt;
                AmazonDriveControl.indexpath = indexpath;

                string itembasepath;
                try
                {
                    var loginjob = Login();
                    var initjob = AmazonDriveControl.InitAlltree(loginjob);
                    initjob.Wait(ct: ct);
                    target = FindItems(remotepath?.Split('/'));

                    var target2 = target.SelectMany(x => DriveData.GetAllChildrenfromId(x.id));
                    itembasepath = FormMatch.GetBasePath(target.Select(x => DriveData.GetFullPathfromId(x.id)).Distinct());
                    target = target2.Where(x => x.kind == "FILE").ToArray();

                    if (target.Length < 1)
                    {
                        masterjob.Result = 2;
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    masterjob.Result = -1;
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    masterjob.Result = 1;
                    return;
                }

                if (ignorespath != null)
                {
                    var targetdict = new ConcurrentDictionary<string, FileMetadata_Info>();
                    Parallel.ForEach(target, item =>
                    {
                        targetdict[DriveData.GetFullPathfromId(item.id)] = item;
                    });
                    Console.WriteLine("ignore list loading...");
                    using (var file = new FileStream(ignorespath, FileMode.Open))
                    using (var sr = new StreamReader(file))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine().Split('\t');
                            FileMetadata_Info o;
                            if (line.Length > 1)
                            {
                                if (targetdict.TryGetValue(line[0], out o))
                                {
                                    if(o.contentProperties?.md5 == line[1])
                                    {
                                        if (targetdict.TryRemove(line[0], out o))
                                            Console.WriteLine(line[0]);
                                    }
                                }
                            }
                            else
                            {
                                if (targetdict.TryRemove(line[0], out o))
                                    Console.WriteLine(line[0]);
                            }
                        }
                    }
                    target = targetdict.Values.ToArray();
                    Console.WriteLine("remain target: " + target.Length);

                    if (target.Length < 1)
                    {
                        masterjob.Result = 2;
                        return;
                    }
                }

                bool SelectFullpath = false;
                if (String.IsNullOrEmpty(localpath))
                {
                    Thread t = new Thread(new ThreadStart(() =>
                    {
                        if (target.Length > 1)
                        {
                            using (var save = new FolderBrowserDialog())
                            {
                                save.Description = "Select Save Folder for Download Items";
                                if (save.ShowDialog() != DialogResult.OK) return;
                                localpath = save.SelectedPath;
                            }

                        }
                        else
                        {
                            using (var save = new SaveFileDialog())
                            {
                                var filename = DriveData.AmazonDriveTree[target[0].id].DisplayName;
                                save.FileName = filename;
                                if (save.ShowDialog() != DialogResult.OK) return;
                                localpath = save.FileName;
                                SelectFullpath = true;
                            }
                        }
                    }));
                    t.SetApartmentState(System.Threading.ApartmentState.STA);
                    t.Start();
                    t.Join();
                    if (localpath == null)
                    {
                        masterjob.Result = 0;
                        return;
                    }
                }

                try
                {
                    Console.Error.WriteLine("remote:" + remotepath);
                    Console.Error.WriteLine("local:" + localpath);
                    if (indexpath != null)
                        Console.Error.WriteLine("index:" + indexpath);

                    if (target.Length == 1)
                    {
                        var filename = DriveData.AmazonDriveTree[target[0].id].DisplayName;
                        if (!SelectFullpath)
                            localpath = Path.Combine(localpath, filename);
                    }
                    if (target.Length > 1 && Path.GetFileName(localpath) != "")
                    {
                        localpath += "\\";
                    }

                    ConsoleJobDisp.Run();

                    var jobs = AmazonDriveControl.downloadItems(target, localpath, masterjob);

                    int errorcount = 0;
                    Task.WaitAll(jobs.Select(x => x.WaitTask(ct: ct)).ToArray());
                    foreach(var j2 in jobs)
                    {
                        if (j2.IsError) errorcount++;
                    }
                    masterjob.Result = (errorcount == 0) ? 0 : errorcount + 10;
                }
                catch (OperationCanceledException)
                {
                    masterjob.Result = -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    masterjob.Result = 1;
                }
            });
            try
            {
                masterjob.Wait(ct: ct);
            }
            catch (OperationCanceledException)
            {
            }
            Config.IsClosing = true;
            Console.Out.Flush();
            return (masterjob.Result as int?) ?? -1;
        }


        private static JobControler.Job[] DoUpload(string uploadpath, string target_id, JobControler.Job prevJob)
        {
            if (File.Exists(uploadpath))
            {
                return AmazonDriveControl.DoFileUpload(new string[] { uploadpath }, target_id, WeekDepend: true, parentJob: prevJob);
            }
            else if (Directory.Exists(uploadpath))
            {
                return AmazonDriveControl.DoDirectoryUpload(new string[] { uploadpath }, target_id, WeekDepend: true, parentJob: prevJob);
            }
            return null;
        }

        private static class UploadParam
        {
            public static string watchdir;
            public static string localbasepath;
            public static string target_id;
            public static JobControler.Job master;
        }

        static List<string> changes_list = new List<string>();

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            var target = Path.GetFileName(e.FullPath);
            target = Path.Combine(UploadParam.watchdir, target);
            string uploadpath = "";

            while (true)
            {
                try
                {
                    using (var f = new FileStream(e.FullPath, FileMode.Open))
                    using (var sr = new StreamReader(f))
                    {
                        uploadpath = sr.ReadLine();
                        break;
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
            lock (changes_list)
            {
                if (changes_list.Contains(uploadpath))
                {
                    Console.Error.WriteLine("already added."+uploadpath);
                    return;
                }
                changes_list.Add(uploadpath);
            }

            Task.Delay(TimeSpan.FromSeconds(10), UploadParam.master.ct).Wait(UploadParam.master.ct);

            var prevJob = UploadParam.master;
            var target_id = UploadParam.target_id;
            if (uploadpath.StartsWith(UploadParam.localbasepath))
            {
                var targetpath = Path.GetDirectoryName(uploadpath);
                targetpath = targetpath.Substring(UploadParam.localbasepath.Length);
                if (!string.IsNullOrEmpty(targetpath))
                {
                    target_id = AmazonDriveControl.CreateDirectory(targetpath, target_id, ct: UploadParam.master.ct);
                    if(target_id == null)
                    {
                        Console.Error.WriteLine("CreateFolder failed.");
                        return;
                    }
                }
            }
            var upjob = AmazonDriveControl.DoFileUpload(new string[] { uploadpath }, target_id, WeekDepend: true, parentJob: prevJob);
            var cleanjob = JobControler.CreateNewJob(JobControler.JobClass.Clean, depends: upjob);
            cleanjob.DisplayName = "clean file";
            cleanjob.DoAlways = true;
            JobControler.Run(cleanjob, (j) =>
            {
                if (upjob.First().IsCanceled) return;

                while (File.Exists(e.FullPath))
                {
                    try
                    {
                        File.Delete(e.FullPath);
                    }
                    catch
                    {
                        Task.Delay(1000, cleanjob.ct).Wait(cleanjob.ct);
                    }
                }
                if ((upjob.First().Result as int? ?? -1) < 0)
                {
                    while (true)
                    {
                        try
                        {
                            using (var f = new FileStream(uploadpath + ".err.log", FileMode.Create))
                            using (var sw = new StreamWriter(f))
                            {
                                sw.WriteLine(upjob.First().DisplayName);
                                sw.WriteLine(upjob.First().ProgressStr);
                                break;
                            }
                        }
                        catch
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                else
                {
                    while (File.Exists(uploadpath))
                    {
                        try
                        {
                            File.Delete(uploadpath);
                        }
                        catch
                        {
                            Task.Delay(1000, cleanjob.ct).Wait(cleanjob.ct);
                        }
                    }
                }
                lock (changes_list)
                {
                    changes_list.Remove(uploadpath);
                }
            });
        }

        static int Upload(string[] targetArgs, string[] paramArgs, bool watchflag = false)
        {
            var masterjob = JobControler.CreateNewJob(JobControler.JobClass.ControlMaster);
            masterjob.DisplayName = "upload";
            var ct = masterjob.ct;
            JobControler.Run(masterjob, (j) =>
            {
                string remotepath = null;
                string localpath = null;
                string watchdir = null;
                string localbasepath = null;
                string target_id = null;

                bool createdir = false;
                bool hashflag = false;
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "md5":
                            Console.Error.WriteLine("(--md5: hash check mode)");
                            hashflag = true;
                            break;
                        case "createpath":
                            Console.Error.WriteLine("(--createpath: make upload target folder mode)");
                            createdir = true;
                            break;
                        case "crypt1":
                            Console.Error.WriteLine("(--crypt1: crypt upload mode(CTR mode))");
                            Config.UseEncryption = true;
                            Config.UseFilenameEncryption = false;
                            Config.CryptMethod = CryptMethods.Method1_CTR;
                            break;
                        case "nocrypt":
                            Console.Error.WriteLine("(--nocrypt: nomal upload mode)");
                            Config.UseEncryption = false;
                            Config.UseFilenameEncryption = false;
                            break;
                        case "crypt1name":
                            Console.Error.WriteLine("(--crypt1name: crypt filename(CTR mode))");
                            Config.UseFilenameEncryption = true;
                            Config.UseEncryption = true;
                            Config.CryptMethod = CryptMethods.Method1_CTR;
                            break;
                        case "crypt2":
                            Console.Error.WriteLine("(--crypt2: crypt upload mode(CBC mode CarrotDAV))");
                            Config.UseFilenameEncryption = true;
                            Config.UseEncryption = true;
                            Config.CryptMethod = CryptMethods.Method2_CBC_CarotDAV;
                            break;
                    }
                }

                AmazonDriveControl.checkMD5 = hashflag;
                AmazonDriveControl.overrideConflict = true;
                AmazonDriveControl.upskip_check = true;

                if (watchflag)
                {
                    if (targetArgs.Length > 3)
                    {
                        remotepath = targetArgs[3];
                        remotepath = remotepath.Replace('\\', '/');

                    }
                    if (targetArgs.Length > 2)
                    {
                        localbasepath = targetArgs[2];
                        if (Path.GetFileName(localbasepath) == "")
                            localbasepath = localbasepath.Substring(0, localbasepath.Length - 1);
                        localbasepath = Path.GetFullPath(localbasepath);
                    }
                    if (targetArgs.Length > 1)
                        watchdir = targetArgs[1];

                    if (string.IsNullOrEmpty(remotepath) || string.IsNullOrEmpty(watchdir) || string.IsNullOrEmpty(localbasepath))
                    {
                        masterjob.Result = 0;
                        return;
                    }
                }
                else
                {
                    if (targetArgs.Length > 2)
                    {
                        remotepath = targetArgs[2];
                        remotepath = remotepath.Replace('\\', '/');

                    }
                    if (targetArgs.Length > 1)
                        localpath = targetArgs[1];

                    if (string.IsNullOrEmpty(remotepath) || string.IsNullOrEmpty(localpath))
                    {
                        masterjob.Result = 0;
                        return;
                    }
                }
                try
                {
                    var loginjob = Login();
                    var initjob = AmazonDriveControl.InitAlltree(loginjob);
                    initjob.Wait(ct: ct);
                    target_id = FindItemsID(remotepath?.Split('/'));

                    if (string.IsNullOrEmpty(target_id) && !createdir)
                    {
                        masterjob.Result = 2;
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    masterjob.Result = -1;
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    masterjob.Result = 1;
                    return;
                }

                try
                {
                    if (string.IsNullOrEmpty(target_id) && createdir)
                    {
                        Console.Error.WriteLine("create path:" + remotepath);
                        target_id = AmazonDriveControl.CreateDirectory(remotepath, DriveData.AmazonDriveRootID, ct: ct);
                    }
                    if (string.IsNullOrEmpty(target_id))
                    {
                        Console.Error.WriteLine("error: createFolder");
                        masterjob.Result = -1;
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    masterjob.Result = -1;
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    masterjob.Result = 1;
                    return;
                }

                ConsoleJobDisp.Run();

                try
                {
                    if (watchdir == null)
                    {
                        Console.Error.WriteLine("remote:" + remotepath);
                        Console.Error.WriteLine("local:" + localpath);

                        var jobs = DoUpload(localpath, target_id, masterjob);
                        Task.WaitAll(jobs.Select(x => x.WaitTask(ct: ct)).ToArray());
                        masterjob.Result = jobs.Select(x => x.Result as int?).Max();
                    }
                    else
                    {
                        Console.Error.WriteLine("remote:" + remotepath);
                        Console.Error.WriteLine("watch:" + watchdir);

                        UploadParam.target_id = target_id;
                        UploadParam.watchdir = watchdir;
                        UploadParam.localbasepath = localbasepath;
                        UploadParam.master = masterjob;

                        // Create a new FileSystemWatcher and set its properties.
                        FileSystemWatcher watcher = new FileSystemWatcher();
                        watcher.Path = watchdir;

                        // Add event handlers.
                        watcher.Created += new FileSystemEventHandler(OnChanged);

                        // Begin watching.
                        watcher.EnableRaisingEvents = true;

                        try
                        {
                            Task.Delay(-1, ct).Wait(ct);
                        }
                        catch
                        {
                            watcher.EnableRaisingEvents = false;
                            throw;
                        }
                        masterjob.Result = 0;
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    masterjob.Result = -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    masterjob.Result = 1;
                }
            });
            try
            {
                masterjob.Wait(ct: ct);
            }
            catch (OperationCanceledException)
            {
            }
            Config.IsClosing = true;
            Console.Out.Flush();
            return (masterjob.Result as int?) ?? -1;
        }
        ///////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetConsoleOutputCP();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(StandardHandle nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(StandardHandle nStdHandle, IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern FileType GetFileType(IntPtr handle);

        private enum StandardHandle : uint
        {
            Input = unchecked((uint)-10),
            Output = unchecked((uint)-11),
            Error = unchecked((uint)-12)
        }

        private enum FileType : uint
        {
            Unknown = 0x0000,
            Disk = 0x0001,
            Char = 0x0002,
            Pipe = 0x0003
        }

        private static bool IsRedirected(IntPtr handle)
        {
            FileType fileType = GetFileType(handle);

            return (fileType == FileType.Disk) || (fileType == FileType.Pipe);
        }
    }
}
