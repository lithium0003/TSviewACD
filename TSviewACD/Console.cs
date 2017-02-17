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

        public async static Task<int> MainFunc(string[] args)
        {
            //http://www.zghthy.com/1322994/codep1/attachconsole-shows-data-on-pipe-but-the-%3E-operator-doesnt-correctly-redirect-on-file
            if (IsRedirected(GetStdHandle(StandardHandle.Output)))
            {
                var initialiseOut = Console.Out;
            }

            bool errorRedirected = IsRedirected(GetStdHandle(StandardHandle.Error));
            if (errorRedirected)
            {
                var initialiseError = Console.Error;
            }

            if (!AttachConsole(-1))
                AllocConsole();

            if (!errorRedirected)
                SetStdHandle(StandardHandle.Error, GetStdHandle(StandardHandle.Output));

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
                    Console.WriteLine("\tdownload (REMOTE_PATH) (LOCAL_DIR_PATH)   : download item");
                    Console.WriteLine("\t\t--md5 : hash check after download");
                    Console.WriteLine("\t\t--cryptname: crypt name mode");
                    Console.WriteLine("\t\t--plainname: plain name mode");
                    Console.WriteLine("\tupload   (LOCAL_FILE_PATH) (REMOTE_PATH)  : upload item");
                    Console.WriteLine("\t\t--md5 : hash check after upload");
                    Console.WriteLine("\t\t--crypt: crypt upload mode");
                    Console.WriteLine("\t\t--nocrypt: nomal upload mode");
                    Console.WriteLine("\t\t--cryptname: crypt name mode");
                    Console.WriteLine("\t\t--plainname: plain name mode");
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
            foreach (var task in ConsoleTasks)
                task.cts.Cancel();
            args.Cancel = true;
            await Task.Run(() =>
            {
                while (ConsoleTasks.Count > 0)
                    Thread.Sleep(100);
            }).ConfigureAwait(false);
        }


        private static async Task Login()
        {
            var task = new TaskCanselToken("Login");
            ConsoleTasks.Add(task);
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
                ConsoleTasks.Remove(task);
            }
        }


        static async Task<FileMetadata_Info[]> FindItems(string[] path_str, FileMetadata_Info root = null, CancellationToken ct = default(CancellationToken))
        {
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = new TaskCanselToken("FindItems");
                ConsoleTasks.Add(task);
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

                var children = DriveData.AmazonDriveTree[root.id].children.Select(x => x.Value.info);

                foreach (var c in children)
                {
                    if (c.name == path_str[0]
                        ||
                        ((path_str[0].Contains('*') || path_str[0].Contains('?'))
                                && Regex.IsMatch(c.name, Regex.Escape(path_str[0]).Replace("\\*", ".*").Replace("\\?", "."))))
                    {
                        if (c.kind == "FOLDER")
                            ret.AddRange(await FindItems(path_str.Skip(1).ToArray(), c, ct: ct).ConfigureAwait(false));
                        else
                        {
                            if (path_str[0] == c.name
                                ||
                                (((path_str[0].Contains('*') || path_str[0].Contains('?'))
                                    && Regex.IsMatch(c.name, Regex.Escape(path_str[0]).Replace("\\*", ".*").Replace("\\?", ".")))))
                            {
                                ret.Add(c);
                            }
                        }
                    }
                }
                ret.Sort((x, y) => x.name.CompareTo(y.name));
                return ret.ToArray();
            }
            finally
            {
                if(task != null)
                    ConsoleTasks.Remove(task);
            }
        }


        static async Task<string> FindItemsID(string[] path_str, FileMetadata_Info root = null, CancellationToken ct = default(CancellationToken))
        {
            TaskCanselToken task = null;
            if (ct == default(CancellationToken))
            {
                task = new TaskCanselToken("FindItemsID");
                ConsoleTasks.Add(task);
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

                var children = DriveData.AmazonDriveTree[root.id].children.Select(x => x.Value.info);

                foreach (var c in children)
                {
                    if (c.name == path_str[0])
                    {
                        if (c.kind == "FOLDER")
                            return await FindItemsID(path_str.Skip(1).ToArray(), c).ConfigureAwait(false);
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
                    ConsoleTasks.Remove(task);
            }
        }

        static async Task<int> ListItems(string[] targetArgs, string[] paramArgs)
        {
            var task = new TaskCanselToken("ListItems");
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
                    target = await FindItems(remotepath?.Split('/'), ct: task.cts.Token).ConfigureAwait(false);

                    if (target.Length < 1) return 2;

                    Console.Error.WriteLine("Found : " + target.Length);
                    foreach (var item in target)
                    {
                        Console.WriteLine(item.name + ((item.kind == "FOLDER") ? "/" : ""));
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
                ConsoleTasks.Remove(task);
            }
        }

        static async Task<int> Download(string[] targetArgs, string[] paramArgs)
        {
            var task = new TaskCanselToken("Download");
            ConsoleTasks.Add(task);
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
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "md5":
                            Console.Error.WriteLine("(--md5: hash check mode)");
                            hashflag = true;
                            break;
                        case "cryptname":
                            Console.Error.WriteLine("(--cryptname: crypt name mode)");
                            Config.UseFilenameEncryption = true;
                            break;
                        case "plainname":
                            Console.Error.WriteLine("(--plainname: plain name mode)");
                            Config.UseFilenameEncryption = false;
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

                bool CryptFlag = false;
                string enckey = null;
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
                                var filename = target[0].name;
                                if (Config.UseFilenameEncryption)
                                {
                                    enckey = DriveData.DecryptFilename(target[0]);
                                    if (enckey != null)
                                    {
                                        filename = Path.GetFileNameWithoutExtension(enckey); //.random
                                        CryptFlag = true;
                                    }
                                }
                                if (enckey == null && Path.GetExtension(filename) == ".enc")
                                {
                                    CryptFlag = true;
                                    enckey = Path.GetFileNameWithoutExtension(filename); //.enc
                                    filename = Path.GetFileNameWithoutExtension(enckey); //.random
                                }
                                else if(enckey == null)
                                {
                                    CryptFlag = false;
                                }
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
                        var filename = target[0].name;
                        if (Config.UseFilenameEncryption)
                        {
                            enckey = DriveData.DecryptFilename(target[0]);
                            if (enckey != null)
                            {
                                filename = Path.GetFileNameWithoutExtension(enckey); //.random
                                CryptFlag = true;
                            }
                        }
                        if (enckey == null && Path.GetExtension(filename) == ".enc")
                        {
                            CryptFlag = true;
                            enckey = Path.GetFileNameWithoutExtension(filename); //.enc
                            filename = Path.GetFileNameWithoutExtension(enckey); //.random
                        }
                        else if(enckey == null)
                        {
                            CryptFlag = false;
                        }
                        localpath = Path.Combine(localpath, filename);
                    }
                    if (target.Length > 1 && Path.GetFileName(localpath) != "")
                    {
                        localpath += "\\";
                    }

                    var f_cur = 0;
                    foreach (var downitem in target)
                    {
                        Console.Error.WriteLine("Download : " + downitem.name);
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
                            var filename = downitem.name;
                            if (Config.UseFilenameEncryption)
                            {
                                enckey = DriveData.DecryptFilename(downitem);
                                if (enckey != null)
                                {
                                    filename = Path.GetFileNameWithoutExtension(enckey); //.random
                                    CryptFlag = true;
                                }
                            }
                            if (enckey == null && Path.GetExtension(filename) == ".enc")
                            {
                                CryptFlag = true;
                                enckey = Path.GetFileNameWithoutExtension(filename); //.enc
                                filename = Path.GetFileNameWithoutExtension(enckey); //.random
                            }
                            else if(enckey == null)
                            {
                                CryptFlag = false;
                            }
                            savefilename = Path.Combine(dpath, filename);
                        }

                        var retry = 5;
                        while (--retry > 0)
                        {
                            Func<Stream, Task> dodownload = async (outfile) => {
                                using (var ret = await Drive.downloadFile(downitem, enckey: enckey, ct: ct).ConfigureAwait(false))
                                using (var f = new PositionStream(ret, downitem.contentProperties.size.Value))
                                {
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        Console.Error.Write("\r{0,-79}", download_str + evnt.Log);
                                    };
                                    await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct).ConfigureAwait(false);
                                }
                            };

                            try
                            {
                                using (var outfile = File.Open(savefilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    Console.Error.WriteLine("");
                                    if (downitem.contentProperties.size > ConfigAPI.FilenameChangeTrickSize)
                                    {
                                        Console.Error.WriteLine("Download : <BIG FILE> temporary filename change");
                                        try
                                        {
                                            var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id).ConfigureAwait(false);
                                            await dodownload(outfile).ConfigureAwait(false);
                                        }
                                        finally
                                        {
                                            await Drive.renameItem(downitem.id, downitem.name).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        await dodownload(outfile).ConfigureAwait(false);
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
                                        if (CryptFlag)
                                        {
                                            using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, enckey))
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
                        enckey = null;
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
                ConsoleTasks.Remove(task);
            }
        }

        static async Task<int> Upload(string[] targetArgs, string[] paramArgs)
        {
            var task = new TaskCanselToken("Upload");
            ConsoleTasks.Add(task);
            var ct = task.cts.Token;
            try
            {
                string remotepath = null;
                string localpath = null;
                string target_id = null;

                bool hashflag = false;
                foreach (var p in paramArgs)
                {
                    switch (p)
                    {
                        case "md5":
                            Console.Error.WriteLine("(--md5: hash check mode)");
                            hashflag = true;
                            break;
                        case "crypt":
                            Console.Error.WriteLine("(--crypt: crypt upload mode)");
                            Config.UseEncryption = true;
                            break;
                        case "nocrypt":
                            Console.Error.WriteLine("(--nocrypt: nomal upload mode)");
                            Config.UseEncryption = false;
                            break;
                        case "cryptname":
                            Console.Error.WriteLine("(--cryptname: crypt name mode)");
                            Config.UseFilenameEncryption = true;
                            Config.UseEncryption = false;
                            break;
                        case "plainname":
                            Console.Error.WriteLine("(--plainname: plain name mode)");
                            Config.UseFilenameEncryption = false;
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

                    if (string.IsNullOrEmpty(target_id)) return 2;
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
                    var uploadfilename = (Config.UseEncryption) ? enckey + ".enc" : short_filename;
                    string md5string = null;

                    var checkpoint = DriveData.ChangeCheckpoint;

                    if (done_files?.Select(x => x.name).Contains(uploadfilename) ?? false)
                    {
                        var target = done_files.First(x => x.name == uploadfilename);
                        if (new FileInfo(localpath).Length == target.contentProperties?.size)
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
                                    using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, Path.GetFileNameWithoutExtension(uploadfilename)))
                                    {
                                        await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
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
                        try
                        {
                            await Drive.TrashItem(target.id, ct).ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
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
                                using (var encfile = new AES256CTR_CryptStream(hfile, Config.DrivePassword, Path.GetFileNameWithoutExtension(uploadfilename)))
                                {
                                    await Task.Run(() => { md5 = md5calc.ComputeHash(encfile); }, ct).ConfigureAwait(false);
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

                    if (Config.UseFilenameEncryption)
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
                ConsoleTasks.Remove(task);
            }
        }
        ///////////////////////////////////////////////////////////////////////////////////
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
