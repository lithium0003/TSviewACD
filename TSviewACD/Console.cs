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
        private static AmazonDrive Drive = null;
        private static List<TaskCanselToken> ConsoleTasks = new List<TaskCanselToken>();

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
                    Console.WriteLine("\tupload   (LOCAL_FILE_PATH) (REMOTE_PATH)  : upload item");
                    Console.WriteLine("\t\t--md5 : hash check after upload");
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
                List<FileMetadata_Info> ret = new List<FileMetadata_Info>();
                if (root == null)
                {
                    Console.Error.WriteLine("loading Drive tree...");
                    Console.Error.WriteLine("root...");
                    // Load Root
                    root = (await Drive.ListMetadata("isRoot:true", ct: ct).ConfigureAwait(false)).data[0];
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

                Console.Error.WriteLine("loading child of " + root.name);
                // add tree Root
                // Load Children
                var children = (await Drive.ListChildren(root.id, ct: ct).ConfigureAwait(false)).data;

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
                if (path_str.Length == 0) return root?.id;
                while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
                {
                    path_str = path_str.Skip(1).ToArray();
                }
                if (path_str.Length == 0) return root?.id;

                if (root == null)
                {
                    Console.Error.WriteLine("loading Drive tree...");
                    Console.Error.WriteLine("root...");
                    // Load Root
                    root = (await Drive.ListMetadata("isRoot:true", ct: ct).ConfigureAwait(false)).data[0];
                }

                Console.Error.WriteLine("loading child of " + root.name);
                // add tree Root
                // Load Children
                var children = (await Drive.ListChildren(root.id, ct: ct).ConfigureAwait(false)).data;

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
                    Drive = new AmazonDrive();
                    await Login().ConfigureAwait(false);
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
                    }
                }

                try
                {
                    Drive = new AmazonDrive();
                    await Login().ConfigureAwait(false);
                    target = await FindItems(remotepath.Split('/'), ct: ct).ConfigureAwait(false);

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
                                save.FileName = target[0].name;
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

                    if (target.Length == 1 && Path.GetFileName(localpath) == "")
                    {
                        localpath = Path.Combine(localpath, target[0].name);
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

                        var savefilename = (target.Length > 1) ? localpath + downitem.name : localpath;
                        var retry = 5;
                        while (--retry > 0)
                            try
                            {
                                using (var outfile = File.Open(savefilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    Console.Error.WriteLine("");
                                    if (downitem.contentProperties.size > 10 * 1024 * 1024 * 1024L)
                                    {
                                        Console.Error.WriteLine("Download : <BIG FILE> temporary filename change");
                                        try
                                        {
                                            var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id);
                                            var ret = await Drive.downloadFile(downitem.id, ct: ct);
                                            var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                            f.PosChangeEvent += (src, evnt) =>
                                            {
                                                Console.Error.Write("\r{0,-79}", download_str + evnt.Log);
                                            };
                                            await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct);
                                        }
                                        finally
                                        {
                                            await Drive.renameItem(downitem.id, downitem.name);
                                        }
                                    }
                                    else
                                    {
                                        var ret = await Drive.downloadFile(downitem.id, ct: ct);
                                        var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                        f.PosChangeEvent += (src, evnt) =>
                                        {
                                            Console.Error.Write("\r{0,-79}", download_str + evnt.Log);
                                        };
                                        await f.CopyToAsync(outfile, 16 * 1024 * 1024, ct);
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
                                        await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct);
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
                ConsoleTasks.Remove(task);
            }
        }

        static async Task<int> Upload(string[] targetArgs, string[] paramArgs)
        {
            var task = new TaskCanselToken("Download");
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
                    Drive = new AmazonDrive();
                    await Login().ConfigureAwait(false);
                    target_id = await FindItemsID(remotepath.Split('/'), ct: ct);

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

                    FileMetadata_Info[] done_files = (await Drive.ListChildren(target_id, ct: ct).ConfigureAwait(false)).data;

                    var upload_str = "Upload...";
                    var short_filename = Path.GetFileName(localpath);
                    string md5string = null;


                    if (done_files?.Select(x => x.name).Contains(short_filename) ?? false)
                    {
                        var target = done_files.First(x => x.name == short_filename);
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
                                await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct);
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
                            await Drive.TrashItem(target.id, ct);
                            await Task.Delay(TimeSpan.FromSeconds(5), ct);
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
                            await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct);
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
                                localpath,
                                target_id,
                                (src, evnt) =>
                                {
                                    Console.Error.Write("\r{0,-79}", upload_str + evnt.Log);
                                },
                                ct);
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
                            await Drive.TrashItem(ret.id, ct);
                            await Task.Delay(TimeSpan.FromSeconds(5), ct);
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
                                await Task.Delay(TimeSpan.FromSeconds(10), ct);

                                var children = await Drive.ListChildren(target_id, ct: ct);
                                if (children.data.Select(x => x.name).Contains(short_filename))
                                {
                                    Console.Error.WriteLine("Upload : child found.");
                                    if (!hashflag)
                                        break;
                                    var uploadeditem = children.data.Where(x => x.name == short_filename).First();
                                    if (uploadeditem.contentProperties.md5 != md5string)
                                    {
                                        Console.Error.WriteLine("Upload : but hash is not match. retry..." + retry.ToString());
                                        checkretry = 0;
                                        Console.Error.WriteLine("conflict.");
                                        Console.Error.WriteLine("remove item...");
                                        await Drive.TrashItem(uploadeditem.id, ct);
                                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
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
