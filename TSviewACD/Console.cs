using System;
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
        private static List<TaskCanselToken> ConsoleTasks = new List<TaskCanselToken>();

        private static Dictionary<string, ItemInfo> DriveTree = new Dictionary<string, ItemInfo>();
        private static List<Changes_Info> treedata = new List<Changes_Info>();

        static public TaskCanselToken CreateTask(string taskname)
        {
            var task = new TaskCanselToken(taskname);
            ConsoleTasks.Add(task);
            return task;
        }

        static public void FinishTask(TaskCanselToken task)
        {
            ConsoleTasks.Remove(task);
        }

        static public async Task CancelTask(string taskname)
        {
            foreach (var item in ConsoleTasks.Where(x => x.taskname == taskname).ToArray())
            {
                item.cts.Cancel();
            }
            while (ConsoleTasks.Where(x => x.taskname == taskname).Count() > 0)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        static public bool CancelTaskAll()
        {
            if (ConsoleTasks.Count > 0)
            {
                foreach (var t in ConsoleTasks)
                    t.cts.Cancel();
            }
            return (ConsoleTasks.Count > 0);
        }

        public async static Task<int> MainFunc(string[] args)
        {
            bool outputRedirected = IsRedirected(GetStdHandle(StandardHandle.Output));
            Stream initialOut = null;
            if (outputRedirected)
            {
                initialOut = Console.OpenStandardOutput();
            }

            bool errorRedirected = IsRedirected(GetStdHandle(StandardHandle.Error));
            Stream initialError = null;
            if (errorRedirected)
            {
                initialError = Console.OpenStandardOutput();
            }

            if (!AttachConsole(-1))
                AllocConsole();

            int codepage = GetConsoleOutputCP();
            if (outputRedirected)
            {
                Console.SetOut(new StreamWriter(initialOut, Encoding.GetEncoding(codepage)));
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
                    Console.WriteLine("\tlist     (REMOTE_PATH)                    : list item");
                    Console.WriteLine("\t\t--recursive: recursive mode");
                    Console.WriteLine("\t\t--md5: show MD5 hash");
                    Console.WriteLine("\t\t--nodecrypt: disable auto decrypt");
                    Console.WriteLine("\tdownload (REMOTE_PATH) (LOCAL_DIR_PATH)   : download item");
                    Console.WriteLine("\t\t--md5 : hash check after download");
                    Console.WriteLine("\t\t--nodecrypt: disable auto decrypt");
                    Console.WriteLine("\tupload   (LOCAL_FILE_PATH) (REMOTE_PATH)  : upload item");
                    Console.WriteLine("\t\t--md5 : hash check after upload");
                    Console.WriteLine("\t\t--createpath: make upload target folder mode");
                    Console.WriteLine("\t\t--crypt1: crypt upload mode(CTR mode)");
                    Console.WriteLine("\t\t--crypt1name: crypt filename(CTR mode)");
                    Console.WriteLine("\t\t--crypt2: crypt upload mode(CBC mode CarrotDAV)");
                    Console.WriteLine("\t\t--nocrypt: nomal upload mode");
                    Console.WriteLine("");
                    Console.WriteLine("\t\t--debug : debug log output");
                    break;
                case "list":
                    Console.Error.WriteLine("list...");
                    return await ListItems(targetArgs, paramArgs).ConfigureAwait(false);
                case "download":
                    Console.Error.WriteLine("download...");
                    return await Download(targetArgs, paramArgs).ConfigureAwait(false);
                case "upload":
                    Console.Error.WriteLine("upload...");
                    return await Upload(targetArgs, paramArgs).ConfigureAwait(false);
            }
            return 0;
        }

        async protected static void CtrlC_Handler(object sender, ConsoleCancelEventArgs args)
        {
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Cancel...");
            TaskCanceler.CancelTaskAll();
            args.Cancel = true;
            await Task.Run(() =>
            {
                while (ConsoleTasks.Count > 0)
                    Thread.Sleep(100);
            }).ConfigureAwait(false);
        }


        private static async Task Login()
        {
            var task = TaskCanceler.CreateTask("Login");
            try
            {
                Console.Error.WriteLine("Login Start.");
                // Login & GetEndpoint
                if (await Drive.Login(task.cts.Token).ConfigureAwait(false) &&
                    await Drive.GetEndpoint(task.cts.Token).ConfigureAwait(false))
                {
                    Console.Error.WriteLine("Login done.");
                }
                else
                {
                    Console.Error.WriteLine("Login failed.");
                    throw new ApplicationException("Login failed.");
                }
            }
            finally
            {
                TaskCanceler.FinishTask(task);
            }
        }


        static async Task<FileMetadata_Info[]> FindItems(string[] path_str, bool recursive = false, FileMetadata_Info root = null, CancellationToken ct = default(CancellationToken))
        {
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("FindItems");
                ct = task.cts.Token;
            }
            try
            {
                ct.ThrowIfCancellationRequested();
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
                            ret.AddRange(await FindItems((recursive && path_str[0] == "*")? path_str: path_str.Skip(1).ToArray(), recursive, c.info, ct: ct).ConfigureAwait(false));
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
            finally
            {
                if (task != null)
                    TaskCanceler.FinishTask(task);
            }
        }


        static async Task<string> FindItemsID(string[] path_str, FileMetadata_Info root = null, CancellationToken ct = default(CancellationToken))
        {
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = TaskCanceler.CreateTask("FindItemsID");
                ct = task.cts.Token;
            }
            try
            {
                ct.ThrowIfCancellationRequested();
                if (path_str.Length == 0) return root?.id;
                while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
                {
                    path_str = path_str.Skip(1).ToArray();
                }
                if (path_str.Length == 0) return root?.id;

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
                            return await FindItemsID(path_str.Skip(1).ToArray(), c.info).ConfigureAwait(false);
                        else
                        {
                            return null;
                        }
                    }
                }
                return null;
            }
            finally
            {
                if (task != null)
                    TaskCanceler.FinishTask(task);
            }
        }

        static async Task<int> ListItems(string[] targetArgs, string[] paramArgs)
        {
            var task = TaskCanceler.CreateTask("ListItems");
            ConsoleTasks.Add(task);
            try
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
                    await Login().ConfigureAwait(false);
                    await DriveData.InitDrive(
                        ct: task.cts.Token,
                        inprogress: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        },
                        done: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        });
                    target = await FindItems(remotepath?.Split('/'), recursive: recursive, ct: task.cts.Token).ConfigureAwait(false);

                    if (target.Length < 1) return 2;

                    Console.Error.WriteLine("Found : " + target.Length);
                    foreach (var item in target)
                    {
                        string detail = "";
                        if (showmd5) detail = "\t" + item.contentProperties?.md5;

                        if (recursive)
                            Console.WriteLine(DriveData.GetFullPathfromId(item.id, nodecrypt) + detail);
                        else
                            Console.WriteLine(((nodecrypt)? item.name: DriveData.AmazonDriveTree[item.id].DisplayName) + ((item.kind == "FOLDER") ? "/" : "") + detail);
                    }

                    return 0;
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }
            }
            finally
            {
                Console.Out.Flush();
                TaskCanceler.FinishTask(task);
            }
        }

        static async Task<int> Download(string[] targetArgs, string[] paramArgs)
        {
            var task = TaskCanceler.CreateTask("Download");
            var ct = task.cts.Token;
            try
            {
                string remotepath = null;
                string localpath = null;
                FileMetadata_Info[] target = null;

                if (targetArgs.Length > 2)
                    localpath = targetArgs[2];
                if (targetArgs.Length > 1)
                {
                    remotepath = targetArgs[1];
                    remotepath = remotepath.Replace('\\', '/');
                }

                if (string.IsNullOrEmpty(remotepath))
                {
                    return 0;
                }

                bool hashflag = false;
                bool autodecrypt = true;
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "md5":
                            Console.Error.WriteLine("(--md5: hash check mode)");
                            hashflag = true;
                            break;
                        case "nodecrypt":
                            Console.Error.WriteLine("(--nodecrypt: disable auto decrypt)");
                            autodecrypt = false;
                            break;
                    }
                }

                string itembasepath;
                try
                {
                    await Login().ConfigureAwait(false);
                    await DriveData.InitDrive(
                        ct: task.cts.Token,
                        inprogress: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        },
                        done: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        });
                    target = await FindItems(remotepath.Split('/'), ct: ct).ConfigureAwait(false);

                    var target2 = target.SelectMany(x => DriveData.GetAllChildrenfromId(x.id));
                    itembasepath = FormMatch.GetBasePath(target.Select(x => DriveData.GetFullPathfromId(x.id)));
                    target = target2.Where(x => x.kind == "FILE").ToArray();

                    if (target.Length < 1) return 2;
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }

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
                            }
                        }
                    }));
                    t.SetApartmentState(System.Threading.ApartmentState.STA);
                    t.Start();
                    t.Join();
                    if (localpath == null) return 0;
                }

                try
                {
                    Console.Error.WriteLine("remote:" + remotepath);
                    Console.Error.WriteLine("local:" + localpath);

                    if (target.Length == 1)
                    {
                        var filename = DriveData.AmazonDriveTree[target[0].id].DisplayName;
                        localpath = Path.Combine(localpath, filename);
                    }
                    if (target.Length > 1 && Path.GetFileName(localpath) != "")
                    {
                        localpath += "\\";
                    }

                    var f_cur = 0;
                    foreach (var downitem in target)
                    {
                        var filename = (autodecrypt) ? DriveData.AmazonDriveTree[downitem.id].DisplayName: downitem.name;
                        var cryptflg = (autodecrypt)? DriveData.AmazonDriveTree[downitem.id].IsEncrypted : CryptMethods.Method0_Plain;
                        Console.Error.WriteLine("Download : " + filename);
                        if (!autodecrypt)
                        {
                            if (DriveData.AmazonDriveTree[downitem.id].IsEncrypted == CryptMethods.Method1_CTR)
                            {
                                if (Regex.IsMatch(downitem.name ?? "", "^[\u2800-\u28ff]+$"))
                                {
                                    var decodename = DriveData.DecryptFilename(downitem);
                                    if (decodename != null)
                                    {
                                        Console.WriteLine("decrypted filename : " + decodename);
                                        Console.WriteLine("filename decode nonce : " + downitem.id);
                                    }
                                }
                            }
                        }
                        var download_str = (target.Length > 1) ? string.Format("Download({0}/{1})...", ++f_cur, target.Length) : "Download...";

                        var savefilename = localpath;
                        if (target.Length > 1)
                        {
                            var itempath = DriveData.GetFullPathfromId(downitem.id).Substring(itembasepath.Length).Split('/');
                            var dpath = localpath;
                            foreach (var p in itempath.Take(itempath.Length - 1))
                            {
                                dpath = Path.Combine(dpath, p);
                                if (!Directory.Exists(dpath)) Directory.CreateDirectory(dpath);
                            }
                            savefilename = Path.Combine(dpath, filename);
                        }

                        var retry = 5;
                        while (--retry > 0)
                        {
                            try
                            {
                                Console.Error.WriteLine("");
                                Console.Error.WriteLine("download {0:#,0} byte", downitem.contentProperties.size);

                                using (var outfile = File.Open(savefilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    using (var ret = new AmazonDriveBaseStream(Drive, downitem, autodecrypt))
                                    using (var f = new PositionStream(ret, downitem.OrignalLength ?? 0))
                                    {
                                        f.PosChangeEvent += (src, evnt) =>
                                        {
                                            Console.Error.Write("\r{0,-79}", download_str + evnt.Log);
                                        };
                                        await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct).ConfigureAwait(false);
                                    }
                                }
                                Console.Error.WriteLine("\r\nDownload : done.");

                                if (hashflag && downitem.contentProperties?.md5 != null)
                                {
                                    using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                                    using (var hfile = File.Open(savefilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        byte[] md5 = null;
                                        Console.Error.WriteLine("Hash check start...");
                                        if (cryptflg == CryptMethods.Method1_CTR)
                                        {
                                            string nonce = null;
                                            nonce = DriveData.DecryptFilename(downitem);
                                            if (nonce == null && Path.GetExtension(downitem.name) == ".enc")
                                            {
                                                nonce = Path.GetFileNameWithoutExtension(downitem.name);
                                            }
                                            if (!string.IsNullOrEmpty(nonce))
                                                using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, nonce))
                                                {
                                                    await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                                }
                                            else
                                                await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct).ConfigureAwait(false);
                                        }
                                        else if (cryptflg == CryptMethods.Method2_CBC_CarotDAV)
                                        {
                                            using (var encfile = new CryptCarotDAV.CryptCarotDAV_CryptStream(hfile, Config.DrivePassword))
                                            {
                                                await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                            }
                                        }
                                        else
                                        {
                                            await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct).ConfigureAwait(false);
                                        }
                                        Console.Error.WriteLine("Hash done.");
                                        var md5string = BitConverter.ToString(md5).ToLower().Replace("-", "");
                                        if (md5string == downitem.contentProperties.md5)
                                        {
                                            Console.Error.WriteLine("Hash check is OK.");
                                        }
                                        else
                                        {
                                            Console.Error.WriteLine("Hash check failed. retry..." + retry.ToString());
                                            continue;
                                        }
                                    }
                                }
                                break;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Download : Error");
                                Console.Error.WriteLine(ex.Message);
                                continue;
                            }
                        }

                        if (retry == 0)
                        {
                            Console.Error.WriteLine("Download : Failed.");
                            return 1;
                        }
                        Console.Error.WriteLine("Download : Done.");
                    }
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }
            }
            finally
            {
                Console.Out.Flush();
                TaskCanceler.FinishTask(task);
            }
        }

        static async Task<FileMetadata_Info> CreateDirs(string[] path_str, FileMetadata_Info root = null, CancellationToken ct = default(CancellationToken))
        {
            ct.ThrowIfCancellationRequested();
            if (root == null)
            {
                root = DriveData.AmazonDriveTree[DriveData.AmazonDriveRootID].info;
            }
            if (!(path_str?.Length > 0))
            {
                return root;
            }
            while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
            {
                path_str = path_str.Skip(1).ToArray();
            }
            if (path_str.Length == 0)
            {
                return root;
            }

            var parent = DriveData.AmazonDriveTree[root.id].info;
            var children = DriveData.AmazonDriveTree[root.id].children.Select(x => x.Value.info);
            FileMetadata_Info targetchild = null;
            if(children.Any(x => x.name == path_str[0]))
            {
                targetchild = children.Where(x => x.name == path_str[0]).FirstOrDefault();
            }
            else
            {
                var enckey = path_str[0] + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                var makedirname = path_str[0];

                Console.Error.WriteLine("createFolder: " + makedirname);

                if (Config.UseEncryption)
                {
                    if (Config.CryptMethod == CryptMethods.Method1_CTR)
                    {
                        if (Config.UseFilenameEncryption)
                            makedirname = Path.GetRandomFileName();
                    }
                    else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                    {
                        makedirname = CryptCarotDAV.EncryptFilename(makedirname, Config.DrivePassword);
                    }
                }

                var checkpoint = DriveData.ChangeCheckpoint;
                int retry = 30;
                while (--retry > 0)
                {
                    try
                    {
                        targetchild = await Drive.createFolder(makedirname, parent.id, ct).ConfigureAwait(false);
                        var children2 = await DriveData.GetChanges(checkpoint, ct).ConfigureAwait(false);
                        if (children2?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                        {
                            break;
                        }
                        await Task.Delay(2000).ConfigureAwait(false);
                        continue;
                    }
                    catch (HttpRequestException)
                    {
                        // retry
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    try
                    {
                        var children2 = await DriveData.GetChanges(checkpoint, ct).ConfigureAwait(false);
                        if (children2?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                        {
                            break;
                        }
                        await Task.Delay(2000).ConfigureAwait(false);
                    }
                    catch (HttpRequestException)
                    {
                        // retry
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
                if (Config.UseEncryption && Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                {
                    if (!await DriveData.EncryptFilename(uploadfilename: makedirname, enckey: enckey, checkpoint: checkpoint, ct: ct))
                    {
                        Console.Error.WriteLine("Fail to create crypted folder");
                    }
                }
                var newitems = (await DriveData.GetChanges(checkpoint, ct).ConfigureAwait(false));
            }
            return await CreateDirs(path_str.Skip(1).ToArray(), targetchild, ct).ConfigureAwait(false);
        }

        static async Task<int> Upload(string[] targetArgs, string[] paramArgs)
        {
            var task = TaskCanceler.CreateTask("Upload");
            var ct = task.cts.Token;
            try
            {
                string remotepath = null;
                string localpath = null;
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

                if (targetArgs.Length > 2)
                {
                    remotepath = targetArgs[2];
                    remotepath = remotepath.Replace('\\', '/');

                }
                if (targetArgs.Length > 1)
                    localpath = targetArgs[1];

                if (string.IsNullOrEmpty(remotepath) || string.IsNullOrEmpty(localpath))
                {
                    return 0;
                }
                try
                {
                    await Login().ConfigureAwait(false);
                    await DriveData.InitDrive(
                        ct: task.cts.Token,
                        inprogress: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        },
                        done: (str) =>
                        {
                            Console.Error.WriteLine(str);
                        }).ConfigureAwait(false);
                    target_id = await FindItemsID(remotepath.Split('/'), ct: ct).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(target_id) && !createdir) return 2;
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }

                try
                {
                    if (string.IsNullOrEmpty(target_id) && createdir)
                    {
                        Console.Error.WriteLine("create path:" + remotepath);
                        target_id = (await CreateDirs(remotepath.Split('/'), ct: ct).ConfigureAwait(false)).id;
                    }
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }

                try
                {
                    Console.Error.WriteLine("remote:" + remotepath);
                    Console.Error.WriteLine("local:" + localpath);

                    Console.Error.WriteLine("Upload File: " + localpath);
                    Console.Error.WriteLine("Confrict check");

                    FileMetadata_Info[] done_files = DriveData.AmazonDriveTree[target_id].children.Select(x => x.Value.info).ToArray();

                    var upload_str = "Upload...";
                    var short_filename = Path.GetFileName(localpath);
                    var enckey = short_filename + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                    string md5string = null;

                    string uploadfilename = short_filename;
                    if (Config.UseEncryption)
                    {
                        if (Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            if (Config.UseFilenameEncryption)
                                uploadfilename = Path.GetRandomFileName();
                            else
                                uploadfilename = enckey + ".enc";
                        }
                        else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            uploadfilename = CryptCarotDAV.EncryptFilename(short_filename, Config.DrivePassword);
                            enckey = "";
                        }
                    }

                    var checkpoint = DriveData.ChangeCheckpoint;
                    var filesize = new FileInfo(localpath).Length;
                    if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                    {
                        filesize = filesize + 128 / 8 + 128;
                    }

                    bool dup_flg = done_files?.Select(x => x.name.ToLower()).Contains(short_filename.ToLower()) ?? false;
                    if (Config.UseEncryption)
                    {
                        if (Config.CryptMethod == CryptMethods.Method1_CTR){
                            dup_flg = dup_flg || (done_files?.Select(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename)).Any(x => x) ?? false);
                            if (Config.UseFilenameEncryption)
                            {
                                dup_flg = dup_flg || (done_files?.Select(x =>
                                {
                                    var enc = DriveData.DecryptFilename(x);
                                    if (enc == null) return false;
                                    return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                }).Any(x => x) ?? false);
                            }
                        }
                        if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            dup_flg = dup_flg || (done_files?.Select(x =>
                            {
                                var enc = CryptCarotDAV.DecryptFilename(x.name, Config.DrivePassword);
                                if (enc == null) return false;
                                return enc == short_filename;
                            }).Any(x => x) ?? false);
                        }
                    }

                    if (dup_flg)
                    {
                        var target = done_files.FirstOrDefault(x => x.name == short_filename);
                        if (target == null && Config.UseEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            target = done_files.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename)).FirstOrDefault();
                            if (target == null && Config.UseFilenameEncryption)
                            {
                                target = done_files.Where(x =>
                                {
                                    var enc = DriveData.DecryptFilename(x);
                                    if (enc == null) return false;
                                    return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                }).FirstOrDefault();
                            }
                        }
                        if (target == null && Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            target = done_files.Where(x =>
                            {
                                var enc = CryptCarotDAV.DecryptFilename(x.name, Config.DrivePassword);
                                if (enc == null) return false;
                                return enc == short_filename;
                            }).FirstOrDefault();
                        }
                        if (filesize == target.contentProperties?.size)
                        {
                            if (!hashflag)
                            {
                                Console.Error.WriteLine("Item is already uploaded.");
                                return 99;
                            }
                            using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                            using (var hfile = File.Open(localpath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                byte[] md5 = null;
                                Console.Error.WriteLine("Hash check start...");
                                if (Config.UseEncryption)
                                {
                                    if (Config.CryptMethod == CryptMethods.Method1_CTR)
                                    {
                                        string nonce = null;
                                        if (Config.UseFilenameEncryption)
                                        {
                                            nonce = DriveData.DecryptFilename(target);
                                        }
                                        if (Path.GetExtension(target.name) == ".enc")
                                        {
                                            nonce = Path.GetFileNameWithoutExtension(target.name);
                                        }
                                        if (!string.IsNullOrEmpty(nonce))
                                            using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, nonce))
                                            {
                                                await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                            }
                                    }
                                    else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                    {
                                        using (var encfile = new CryptCarotDAV.CryptCarotDAV_CryptStream(hfile, Config.DrivePassword))
                                        {
                                            await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                        }
                                    }
                                }
                                else
                                {
                                    await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct).ConfigureAwait(false);
                                }
                                Console.Error.WriteLine("Hash done.");
                                md5string = BitConverter.ToString(md5).ToLower().Replace("-", "");
                                if (md5string == target.contentProperties?.md5)
                                {
                                    Console.Error.WriteLine("Item is already uploaded and same Hash.");
                                    return 999;
                                }
                            }
                        }
                        Console.Error.WriteLine("conflict.");
                        Console.Error.WriteLine("remove item...");
                        Config.Log.LogOut("remove item...");
                        try
                        {
                            checkpoint = DriveData.ChangeCheckpoint;
                            foreach (var conflicts in done_files.Where(x => x.name.ToLower() == short_filename.ToLower()))
                            {
                                await Drive.TrashItem(conflicts.id, ct).ConfigureAwait(false);
                            }
                            if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                var conflict_crypt = done_files.Where(x =>
                                {
                                    var enc = CryptCarotDAV.DecryptFilename(x.name, Config.DrivePassword);
                                    if (enc == null) return false;
                                    return enc == short_filename;
                                });
                                foreach (var conflicts in conflict_crypt)
                                {
                                    await Drive.TrashItem(conflicts.id, ct).ConfigureAwait(false);
                                }
                            }
                            if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                if (Config.UseFilenameEncryption)
                                {
                                    var conflict_crypt = done_files.Where(x =>
                                    {
                                        var enc = DriveData.DecryptFilename(x);
                                        if (enc == null) return false;
                                        return Path.GetFileNameWithoutExtension(enc) == short_filename;
                                    });
                                    foreach (var conflicts in conflict_crypt)
                                    {
                                        await Drive.TrashItem(conflicts.id, ct).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    var conflict_crypt = done_files.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename));
                                    foreach (var conflicts in conflict_crypt)
                                    {
                                        await Drive.TrashItem(conflicts.id, ct).ConfigureAwait(false);
                                    }
                                }
                            }
                            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                            await DriveData.GetChanges(checkpoint, ct).ConfigureAwait(false);
                            md5string = null;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (hashflag && md5string == null)
                    {
                        using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                        using (var hfile = File.Open(localpath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] md5 = null;
                            Console.Error.WriteLine("Hash check start...");
                            if (Config.UseEncryption)
                            {
                                if (Config.CryptMethod == CryptMethods.Method1_CTR)
                                {
                                    using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, enckey))
                                    {
                                        await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                    }
                                }
                                else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                {
                                    using (var encfile = new CryptCarotDAV.CryptCarotDAV_CryptStream(hfile, Config.DrivePassword))
                                    {
                                        await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
                                    }
                                }
                            }
                            else
                            {
                                await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct).ConfigureAwait(false);
                            }
                            Console.Error.WriteLine("Hash done.");
                            md5string = BitConverter.ToString(md5).ToLower().Replace("-", "");
                        }
                    }
                    Console.Error.WriteLine("ok. Upload...");
                    Console.Error.WriteLine("");

                    int retry = 6;
                    while (--retry > 0)
                    {
                        int checkretry = 4;
                        try
                        {
                            var ret = await Drive.uploadFile(
                                filename: localpath,
                                uploadname: uploadfilename,
                                uploadkey: enckey,
                                parent_id: target_id,
                                process: (src, evnt) =>
                                {
                                    Console.Error.Write("\r{0,-79}", upload_str + evnt.Log);
                                },
                                ct: ct).ConfigureAwait(false);
                            if (!hashflag)
                                break;
                            if (ret.contentProperties.md5 == md5string)
                            {
                                Console.Error.WriteLine("");
                                Console.Error.WriteLine("hash check OK.");
                                break;
                            }

                            Console.Error.WriteLine("");
                            Console.Error.WriteLine("MD5 hash not match. retry...");

                            Console.Error.WriteLine("remove item...");
                            await Drive.TrashItem(ret.id, ct).ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                            Console.Error.WriteLine("retry to upload..." + retry.ToString());
                            continue;
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.Error.WriteLine("");
                            if (ex.Message.Contains("408 (REQUEST_TIMEOUT)")) checkretry = 6 * 5 + 1;
                            if (ex.Message.Contains("409 (Conflict)")) checkretry = 6 * 5 + 1;
                            if (ex.Message.Contains("504 (GATEWAY_TIMEOUT)")) checkretry = 6 * 5 + 1;
                            if (filesize < Config.SmallFileSize) checkretry = 3;
                            Console.Error.WriteLine("Error: " + ex.Message);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("");
                            checkretry = 3 + 1;
                            Console.Error.WriteLine("Error: " + ex.Message);
                        }

                        Console.Error.WriteLine("Upload faild." + retry.ToString());
                        // wait for retry
                        while (--checkretry > 0)
                        {
                            try
                            {
                                Console.Error.WriteLine("Upload : wait 10sec for retry..." + checkretry.ToString());
                                await Task.Delay(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);

                                var children = (await DriveData.GetChanges(checkpoint, ct).ConfigureAwait(false));
                                if (children.Where(x => x.name.Contains(uploadfilename)).LastOrDefault()?.status == "AVAILABLE")
                                {
                                    Console.Error.WriteLine("Upload : child found.");
                                    if (!hashflag)
                                        break;
                                    var uploadeditem = children.Where(x => x.name == uploadfilename).LastOrDefault();
                                    if (uploadeditem.contentProperties.md5 != md5string)
                                    {
                                        Console.Error.WriteLine("Upload : but hash is not match. retry..." + retry.ToString());
                                        checkretry = 0;
                                        Console.Error.WriteLine("conflict.");
                                        Console.Error.WriteLine("remove item...");
                                        await Drive.TrashItem(uploadeditem.id, ct).ConfigureAwait(false);
                                        await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("Upload : hash check OK.");
                                    }
                                    break;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception)
                            {
                            }
                        }
                        if (checkretry > 0)
                            break;
                    }
                    if (retry == 0)
                    {
                        Console.Error.WriteLine("Upload : failed.");
                        return -1;
                    }

                    if (Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                    {
                        if (!await DriveData.EncryptFilename(uploadfilename: uploadfilename, enckey: enckey, checkpoint: checkpoint, ct: ct).ConfigureAwait(false))
                        {
                            Console.Error.WriteLine("Upload : failed.");
                            return -1;
                        }
                    }

                    Console.Error.WriteLine("");
                    Console.Error.WriteLine("Upload : done.");

                    return 0;
                }
                catch (OperationCanceledException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("error: " + ex.ToString());
                    return 1;
                }
            }
            finally
            {
                Console.Out.Flush();
                TaskCanceler.FinishTask(task);
            }
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
