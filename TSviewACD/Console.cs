﻿using System;
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
            switch (args[0])
            {
                case "help":
                    Console.WriteLine("usage");
                    Console.WriteLine("\thelp                                      : show help");
                    Console.WriteLine("\tdownload (REMOTE_PATH) (LOCAL_DIR_PATH)   : download item");
                    Console.WriteLine("\tupload (LOCAL_FILE_PATH) (REMOTE_PATH)    : upload item");
                    break;
                case "download":
                    Console.Error.WriteLine("download...");
                    return await Download(args).ConfigureAwait(false);
                case "upload":
                    Console.Error.WriteLine("upload...");
                    return await Upload(args);
            }
            return 0;
        }

        protected static void CtrlC_Handler(object sender, ConsoleCancelEventArgs args)
        {
            Console.Error.WriteLine("");
            Console.Error.WriteLine("Cancel...");
            Drive?.Cancel();
            args.Cancel = true;
        }

        private static async Task Login()
        {
            Console.Error.WriteLine("Login Start.");
            // Login & GetEndpoint
            if (await Drive.Login() &&
                await Drive.GetEndpoint())
            {
                Console.Error.WriteLine("Login done.");
            }
            else
            {
                Console.Error.WriteLine("Login failed.");
                throw new ApplicationException("Login failed.");
            }
        }


        static async Task<FileMetadata_Info[]> FindItems(string[] path_str, FileMetadata_Info root = null)
        {
            if (path_str.Length == 0) return null;
            while (path_str.Length > 0 && string.IsNullOrEmpty(path_str.First()))
            {
                path_str = path_str.Skip(1).ToArray();
            }
            if (path_str.Length == 0) return null;

            if (root == null)
            {
                Console.Error.WriteLine("loading Drive tree...");
                Console.Error.WriteLine("root...");
                // Load Root
                root = (await Drive.ListMetadata("isRoot:true").ConfigureAwait(false)).data[0];
            }

            Console.Error.WriteLine("loading child of "+root.name);
            // add tree Root
            // Load Children
            var children = (await Drive.ListChildren(root.id).ConfigureAwait(false)).data;

            List<FileMetadata_Info> ret = new List<FileMetadata_Info>();
            foreach(var c in children)
            {
                if(c.name == path_str[0] 
                    ||
                    ((path_str[0].Contains('*') || path_str[0].Contains('?'))
                            && Regex.IsMatch(c.name, Regex.Escape(path_str[0]).Replace("\\*", ".*").Replace("\\?", "."))))
                {
                    if (c.kind == "FOLDER")
                        ret.AddRange(await FindItems(path_str.Skip(1).ToArray(), c).ConfigureAwait(false));
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
            ret.Sort((x,y) => x.name.CompareTo(y.name));
            return ret.ToArray();
        }


        static async Task<string> FindItemsID(string[] path_str, FileMetadata_Info root = null)
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
                root = (await Drive.ListMetadata("isRoot:true").ConfigureAwait(false)).data[0];
            }

            Console.Error.WriteLine("loading child of " + root.name);
            // add tree Root
            // Load Children
            var children = (await Drive.ListChildren(root.id).ConfigureAwait(false)).data;

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

        static async Task<int> Download(string[] args)
        {
            string remotepath = null;
            string localpath = null;
            FileMetadata_Info[] target = null;

            if (args.Length > 2)
                localpath = args[2];
            if (args.Length > 1)
            {
                remotepath = args[1];
                remotepath = remotepath.Replace('\\', '/');
            }

            if (string.IsNullOrEmpty(remotepath))
            {
                return 0;
            }
            try
            {
                Drive = new AmazonDrive();
                await Login().ConfigureAwait(false);
                target = await FindItems(remotepath.Split('/')).ConfigureAwait(false);

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
                            using (var outfile = File.OpenWrite(savefilename))
                            {
                                Console.Error.WriteLine("");
                                if (downitem.contentProperties.size > 10 * 1024 * 1024 * 1024L)
                                {
                                    Console.Error.WriteLine("Download : <BIG FILE> temporary filename change");
                                    try
                                    {
                                        var tmpfile = await Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id);
                                        var ret = await Drive.downloadFile(downitem.id);
                                        var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                        f.PosChangeEvent += (src, evnt) =>
                                        {
                                            Console.Error.Write('\r' + download_str + evnt.Log + "     ");
                                        };
                                        await f.CopyToAsync(outfile, 16 * 1024 * 1024, Drive.ct);
                                    }
                                    finally
                                    {
                                        await Drive.renameItem(downitem.id, downitem.name);
                                    }
                                }
                                else
                                {
                                    var ret = await Drive.downloadFile(downitem.id);
                                    var f = new PositionStream(ret, downitem.contentProperties.size.Value);
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        Console.Error.Write('\r' + download_str + evnt.Log + "     ");
                                    };
                                    await f.CopyToAsync(outfile, 16 * 1024 * 1024, Drive.ct);
                                }
                            }
                            Console.Error.WriteLine("\r\nDownload : done.");
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
                Console.Error.WriteLine("error: "+ex.ToString());
                return 1;
            }
        }

        static async Task<int> Upload(string[] args)
        {
            string remotepath = null;
            string localpath = null;
            string target_id = null;

            if (args.Length > 2)
            {
                remotepath = args[2];
                remotepath = remotepath.Replace('\\', '/');

            }
            if (args.Length > 1)
                localpath = args[1];

            if (string.IsNullOrEmpty(remotepath) || string.IsNullOrEmpty(localpath))
            {
                return 0;
            }
            try
            {
                Drive = new AmazonDrive();
                await Login().ConfigureAwait(false);
                target_id = await FindItemsID(remotepath.Split('/'));

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

                FileMetadata_Info[] done_files = (await Drive.ListChildren(target_id).ConfigureAwait(false)).data;

                var upload_str = "Upload...";
                var short_filename = Path.GetFileName(localpath);

                if (done_files?.Select(x => x.name).Contains(short_filename) ?? false)
                {
                    var target = done_files.First(x => x.name == short_filename);
                    if (new FileInfo(localpath).Length == target.contentProperties?.size)
                    {
                        Console.Error.WriteLine("Item is already uploaded.");
                        return 99;
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
                                Console.Error.Write('\r' + upload_str + evnt.Log + "     ");
                            });
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
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
                        checkretry = 3 + 1;
                        Console.Error.WriteLine("Error: " + ex.Message);
                    }

                    Console.Error.WriteLine("Upload faild." + retry.ToString());
                    // wait for retry
                    while (--checkretry > 0)
                    {
                        Console.Error.WriteLine("Upload : wait 10sec for retry..." + checkretry.ToString());
                        await Task.Delay(TimeSpan.FromSeconds(10), Drive.ct);

                        var children = await Drive.ListChildren(target_id);
                        if (children.data.Select(x => x.name).Contains(short_filename))
                        {
                            Console.Error.WriteLine("Upload : child found.");
                            break;
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
