using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TSviewACD
{
    class AmazonDriveControl
    {
        static public bool checkMD5 = false;
        static public bool upskip_check = true;
        static public bool overrideConflict = false;
        static public string indexpath = null;
        static public bool autodecrypt = true;

        public enum ReloadType
        {
            Cache,
            GetChanges,
            All,
        }
        static public Action<string, ReloadType> DoReload;

        static public void Reload(string reload_id)
        {
            DoReload?.Invoke(reload_id, ReloadType.GetChanges);
        }

        static public JobControler.Job InitAlltree(JobControler.Job prevJob)
        {
            var job = JobControler.CreateNewJob(depends: prevJob);
            job.DisplayName = "Loading tree data";
            var ct = job.ct;
            JobControler.Run(job, (j) =>
            {
                job.Progress = -1;
                DriveData.InitDrive(ct: ct,
                    inprogress: (str) =>
                    {
                        job.ProgressStr = str;
                    },
                    done: (str) =>
                    {
                        job.ProgressStr = str;
                        job.Progress = 1;
                    }).Wait(ct);
            });
            return job;
        }

        static public JobControler.Job CreateDirectory(string path, string parent_id, JobControler.Job prev_job = null)
        {
            var paths = path.Split('\\', '/');
            // フォルダを確認してなければ作る
            var job = JobControler.CreateNewJob(type: JobControler.JobClass.Normal, depends: prev_job);
            job.WeekDepend = true;
            job.DisplayName = "create folder(s) : " + path;
            job.ProgressStr = "wait for create folder(s).";
            JobControler.Run(job, (j) =>
            {
                var parentID = parent_id;
                job.Progress = -1;
                job.ProgressStr = "Create folder(s).";
                foreach (var p in paths)
                {
                    if (p == "") continue;

                    var done_files = DriveData.AmazonDriveTree[parentID].children.Values.Select(x => x.info).ToArray();
                    FileMetadata_Info newdir = null;
                    if (done_files?.Where(x => x.kind == "FOLDER").Select(x => x.name.ToLower()).Contains(p.ToLower()) ?? false)
                    {
                        newdir = done_files.First(x => x.name.ToLower() == p.ToLower() && x.kind == "FOLDER");
                    }
                    if (newdir == null && Config.UseEncryption)
                    {
                        if (Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
                            var selection = done_files?.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == p));
                            if (selection?.Any() ?? false)
                            {
                                newdir = selection.FirstOrDefault();
                            }
                            if (newdir == null && Config.UseFilenameEncryption)
                            {
                                selection = done_files?.Where(x =>
                                {
                                    var enc = DriveData.DecryptFilename(x);
                                    if (enc == null) return false;
                                    return Path.GetFileNameWithoutExtension(enc) == p;
                                });
                                if (selection?.Any() ?? false)
                                {
                                    newdir = selection.FirstOrDefault();
                                }
                            }
                        }
                        if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                        {
                            var selection = done_files?.Where(x =>
                            {
                                var enc = CryptCarotDAV.DecryptFilename(x.name);
                                if (enc == null) return false;
                                return enc == p;
                            });
                            if (selection?.Any() ?? false)
                            {
                                newdir = selection.FirstOrDefault();
                            }
                        }
                    }
                    if (newdir == null)
                    {
                        var job_mkdir = JobControler.CreateNewJob(type: JobControler.JobClass.Normal);
                        job_mkdir.DisplayName = "create folder : " + p;
                        job_mkdir.ProgressStr = "wait for create folder.";
                        JobControler.Run(job_mkdir, (j2) =>
                        {
                            var ct = (j2 as JobControler.Job).ct;
                            job_mkdir.ProgressStr = "Create folder...";
                            job_mkdir.Progress = -1;
                            var enckey = p + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                            var makedirname = p;

                            if (Config.UseEncryption)
                            {
                                if (Config.CryptMethod == CryptMethods.Method1_CTR)
                                {
                                    if (Config.UseFilenameEncryption)
                                        makedirname = Path.GetRandomFileName();
                                }
                                else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                {
                                    makedirname = CryptCarotDAV.EncryptFilename(p);
                                }
                            }

                            // make subdirectory
                            var checkpoint = DriveData.ChangeCheckpoint;
                            int retry = 30;
                            while (--retry > 0)
                            {
                                bool breakflag = false;
                                DriveData.Drive.createFolder(makedirname, parentID, ct).ContinueWith((t) =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        var e = t.Exception;
                                        e.Flatten().Handle(ex =>
                                        {
                                            return true;
                                        });
                                        e.Handle(ex =>
                                        {
                                            return true;
                                        });
                                        return;
                                    }
                                    newdir = t.Result;
                                    Task.Delay(2000, ct).Wait(ct);
                                    DriveData.GetChanges(checkpoint, ct).ContinueWith((t2) =>
                                    {
                                        var children = t2.Result;
                                        if (children?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                                        {
                                            Config.Log.LogOut("createFolder : child found.");
                                            job_mkdir.ProgressStr = "createFolder success.";
                                            breakflag = true;
                                        }
                                    }, ct).Wait(ct);
                                }, ct).Wait(ct);
                                if (breakflag) break;

                                DriveData.GetChanges(checkpoint, ct).ContinueWith((t2) =>
                                {
                                    var children = t2.Result;
                                    if (children?.Where(x => x.name.Contains(makedirname)).LastOrDefault()?.status == "AVAILABLE")
                                    {
                                        Config.Log.LogOut("createFolder : child found.");
                                        job_mkdir.ProgressStr = "createFolder success.";
                                        if (newdir == null)
                                        {
                                            newdir = children.Where(x => x.name.Contains(makedirname) && x.status == "AVAILABLE").LastOrDefault();
                                        }
                                        breakflag = true;
                                    }
                                    Task.Delay(2000, ct).Wait(ct);
                                }, ct).Wait(ct);

                                if (breakflag) break;
                            }
                            if (retry == 0)
                            {
                                Config.Log.LogOut("createFolder : (ERROR)child not found.");
                                JobControler.ErrorOut("createFolder : (ERROR)child not found. {0}", makedirname);
                                job_mkdir.Error("(ERROR)child not found.");
                                return;
                            }
                            if (Config.UseEncryption && Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                            {
                                DriveData.EncryptFilename(uploadfilename: makedirname, enckey: enckey, checkpoint: checkpoint, ct: ct).ContinueWith((t) =>
                                {
                                    if (!t.Result)
                                    {
                                        Config.Log.LogOut("createFolder : (ERROR)name cryption failed.");
                                        JobControler.ErrorOut("createFolder : (ERROR)name cryption failed. {0}", makedirname);
                                        job_mkdir.Error("(ERROR)name cryption failed.");
                                        return;
                                    }
                                }, ct);

                                if (job_mkdir.IsError) return;
                            }
                            job_mkdir.Progress = 1;
                            Reload(parentID);
                        });
                        job_mkdir.Wait(ct: job.ct);

                        if (newdir == null)
                        {
                            job.Progress = double.NaN;
                            job.IsError = true;
                            return;
                        }
                    }
                    parentID = newdir.id;
                }
                job.Progress = 1;
                job.ProgressStr = "done.";
                job.Result = parentID;
            });
            return job;
        }

        static public JobControler.Job[] DoDirectoryUpload(IEnumerable<string> Filenames, string parent_id = null, bool WeekDepend = false, params JobControler.Job[] parentJob)
        {
            var joblist = new List<JobControler.Job>();

            if (Filenames == null) return joblist.ToArray();

            foreach (var filename in Filenames)
            {
                if (parentJob.Any(x => x.IsCanceled)) return null;
                var job = JobControler.CreateNewJob(type: JobControler.JobClass.Upload, depends: parentJob);
                job.WeekDepend = WeekDepend;
                job.DisplayName = filename;
                job.ProgressStr = "wait for folder upload.";
                var ct = job.ct;
                joblist.Add(job);
                JobControler.Run(job, (j) =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (parent_id == null)
                    {
                        var r = job.ResultOfDepend.Where(x => x != null).FirstOrDefault() as string;
                        parent_id = r;
                        if (parent_id == null)
                        {
                            Config.Log.LogOut("failed to get parent_id" + filename);
                            job.Error("failed to get parent_id.");
                            return;
                        }
                    }
                    job.ProgressStr = "Upload...";
                    job.Progress = -1;

                    var short_name = Path.GetFullPath(filename).Split(new char[] { '\\', '/' }).Last();

                    var mkdirjob = CreateDirectory(short_name, parent_id);
                    mkdirjob.Wait(ct: ct);
                    var newdir_id = mkdirjob.Result as string;

                    if (newdir_id == null)
                    {
                        job.Error("Upload : (ERROR)createFolder");
                        return;
                    }
                    job.Result = newdir_id;
                    job.ProgressStr = "done.";
                    job.Progress = 1;
                });
                DoFileUpload(Directory.EnumerateFiles(filename), parentJob: job);
                DoDirectoryUpload(Directory.EnumerateDirectories(filename), parentJob: job);
            }
            return joblist.ToArray();
        }

        static public JobControler.Job[] DoFileUpload(IEnumerable<string> Filenames, string parent_id = null, bool WeekDepend = false, params JobControler.Job[] parentJob)
        {
            var joblist = new List<JobControler.Job>();
            if (Filenames == null) return joblist.ToArray();
            foreach (var filename in Filenames)
            {
                if (parentJob.Any(x => x.IsCanceled)) return null;
                var job = JobControler.CreateNewJob(type: JobControler.JobClass.Upload, depends: parentJob);
                job.WeekDepend = WeekDepend;
                job.DisplayName = filename;
                job.ProgressStr = "wait for upload.";
                var ct = job.ct;
                joblist.Add(job);
                JobControler.Run(job, (j) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var error_str = "";
                    if (parent_id == null)
                    {
                        var r = job.ResultOfDepend.Where(x => x != null).FirstOrDefault() as string;
                        parent_id = r;
                        if (parent_id == null)
                        {
                            Config.Log.LogOut("failed to get parent_id" + filename);
                            job.Error("failed to get parent_id.");
                            return;
                        }
                    }
                    FileMetadata_Info[] done_files = null;

                    if (upskip_check)
                    {
                        done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                    }

                    Config.Log.LogOut("Upload File: " + filename);

                    var short_filename = System.IO.Path.GetFileName(filename);
                    var enckey = short_filename + "." + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
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
                            uploadfilename = CryptCarotDAV.EncryptFilename(short_filename);
                            enckey = "";
                        }
                    }
                    var checkpoint = DriveData.ChangeCheckpoint;
                    var filesize = new FileInfo(filename).Length;
                    if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                    {
                        filesize = filesize + CryptCarotDAV.BlockSizeByte + CryptCarotDAV.CryptHeaderByte + CryptCarotDAV.CryptFooterByte;
                    }

                    bool dup_flg = done_files?.Select(x => x.name.ToLower()).Contains(short_filename.ToLower()) ?? false;
                    if (Config.UseEncryption)
                    {
                        if (Config.CryptMethod == CryptMethods.Method1_CTR)
                        {
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
                                var enc = CryptCarotDAV.DecryptFilename(x.name);
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
                                var enc = CryptCarotDAV.DecryptFilename(x.name);
                                if (enc == null) return false;
                                return enc == short_filename;
                            }).FirstOrDefault();
                        }

                        if (filesize == target?.contentProperties?.size)
                        {
                            if (!checkMD5)
                            {
                                Config.Log.LogOut("Upload : done. already same size " + uploadfilename);
                                job.ProgressStr = "Upload Done. file already uploaded.";
                                job.Progress = 1;
                                job.Result = 99;
                                return;
                            }

                            using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                            using (var hfile = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                byte[] md5 = null;
                                job.ProgressStr = "Check file MD5...";
                                job.Progress = -1;
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
                                            using (var encfile = new CryptCTR.AES256CTR_CryptStream(hfile, nonce))
                                            {
                                                md5 = md5calc.ComputeHash(encfile);
                                            }
                                    }
                                    else if (Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                                    {
                                        using (var encfile = new CryptCarotDAV.CryptCarotDAV_CryptStream(hfile))
                                        {
                                            md5 = md5calc.ComputeHash(encfile);
                                        }
                                    }
                                }
                                else
                                {
                                    md5 = md5calc.ComputeHash(hfile);
                                }
                                job.ProgressStr = "MD5 Check done.";
                                job.Progress = 0;
                                if (BitConverter.ToString(md5).ToLower().Replace("-", "") == target.contentProperties?.md5)
                                {
                                    Config.Log.LogOut("Upload : done. already same MD5 " + uploadfilename);
                                    job.ProgressStr = "Upload Done. file already uploaded and same MD5.";
                                    job.Progress = 1;
                                    job.Result = 999;
                                    return;
                                }
                            }
                        }
                        Config.Log.LogOut(string.Format("conflict. name:{0} upload:{1}", short_filename, uploadfilename));
                        if (!overrideConflict)
                        {
                            Config.Log.LogOut("Upload : done. conflict and keep orignal " + uploadfilename);
                            JobControler.ErrorOut("Upload Aborted. file already uploaded but confrict. {0}->{1}", filename, uploadfilename);
                            job.ProgressStr = "Upload Aborted. file already uploaded but confrict.";
                            job.Progress = 1;
                            job.Result = -999;
                            return;
                        }
                        Config.Log.LogOut("remove item...");
                        job.ProgressStr = "Upload : remove previous item.";
                        job.Progress = -1;
                        try
                        {
                            checkpoint = DriveData.ChangeCheckpoint;
                            foreach (var conflicts in done_files.Where(x => x.name.ToLower() == short_filename.ToLower()))
                            {
                                DriveData.Drive.TrashItem(conflicts.id, ct).Wait(ct);
                            }
                            if (Config.UseEncryption && Config.CryptMethod == CryptMethods.Method2_CBC_CarotDAV)
                            {
                                var conflict_crypt = done_files.Where(x =>
                                {
                                    var enc = CryptCarotDAV.DecryptFilename(x.name);
                                    if (enc == null) return false;
                                    return enc == short_filename;
                                });
                                foreach (var conflicts in conflict_crypt)
                                {
                                    DriveData.Drive.TrashItem(conflicts.id, ct).Wait(ct);
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
                                        DriveData.Drive.TrashItem(conflicts.id, ct).Wait(ct);
                                    }
                                }
                                else
                                {
                                    var conflict_crypt = done_files.Where(x => (Path.GetExtension(x.name) == ".enc") && (Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(x.name)) == short_filename));
                                    foreach (var conflicts in conflict_crypt)
                                    {
                                        DriveData.Drive.TrashItem(conflicts.id, ct).Wait(ct);
                                    }
                                }
                            }
                            Task.Delay(TimeSpan.FromSeconds(5), ct).ContinueWith((t) =>
                            {
                                DriveData.GetChanges(checkpoint, ct).ContinueWith((t2) =>
                                {
                                    checkpoint = DriveData.ChangeCheckpoint;
                                    done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                                }, ct).Wait(ct);
                            }, ct).Wait(ct);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("remove item ERROR" + ex.Message);
                            job.ProgressStr = "Upload : ERROR remove previous item. " + ex.Message;
                            throw;
                        }
                    }

                    job.ProgressStr = "Upload...";
                    job.Progress = 0;


                    int retry = 6;
                    while (--retry > 0)
                    {
                        int checkretry = 4;
                        string uphash = null;
                        bool breakflag = false;
                        DriveData.Drive.uploadFile(
                            filename: filename,
                            uploadname: uploadfilename,
                            uploadkey: enckey,
                            parent_id: parent_id,
                            process: (src, evnt) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                var eo = evnt;
                                job.ProgressStr = eo.Log;
                                job.Progress = (double)eo.Position / eo.Length;
                            }, ct: ct)
                            .ContinueWith((t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    var e = t.Exception;
                                    e.Flatten().Handle(ex =>
                                    {
                                        if (ex is AmazonDriveUploadException)
                                        {
                                            uphash = ex.Message;
                                            if (ex.InnerException is HttpRequestException)
                                            {
                                                if (ex.InnerException.Message.Contains("408 (REQUEST_TIMEOUT)")) checkretry = 6 * 5 + 1;
                                                if (ex.InnerException.Message.Contains("409 (Conflict)")) checkretry = 3;
                                                if (ex.InnerException.Message.Contains("504 (GATEWAY_TIMEOUT)")) checkretry = 6 * 5 + 1;
                                                if (filesize < Config.SmallFileSize) checkretry = 3;
                                                error_str += ex.InnerException.Message + "\n";
                                            }
                                        }
                                        else
                                        {
                                            error_str += ex.Message + "\n";
                                            checkretry = 3 + 1;
                                        }
                                        return true;
                                    });
                                    e.Handle(ex =>
                                    {
                                        return true;
                                    });
                                    return;
                                }
                                if (t.IsCanceled) return;
                                var tmpDone = done_files.ToList();
                                tmpDone.Add(t.Result);
                                done_files = tmpDone.ToArray();
                                breakflag = true;
                            }).Wait(ct);

                        ct.ThrowIfCancellationRequested();
                        if (breakflag) break;

                        Config.Log.LogOut("Upload failed." + retry.ToString() + " " + uploadfilename);
                        job.ProgressStr = "Upload failed." + retry.ToString();
                        job.Progress = double.NaN;
                        // wait for retry
                        while (--checkretry > 0)
                        {
                            try
                            {
                                Config.Log.LogOut("Upload : wait 10sec for retry..." + checkretry.ToString());
                                job.ProgressStr = "Upload : wait 10sec for retry..." + checkretry.ToString();
                                Task.Delay(TimeSpan.FromSeconds(10), ct).Wait(ct);

                                DriveData.GetChanges(checkpoint, ct).ContinueWith((t) =>
                                {
                                    var children = t.Result;
                                    if (children.Where(x => x.name.Contains(uploadfilename)).LastOrDefault()?.status == "AVAILABLE")
                                    {
                                        Config.Log.LogOut("Upload : child found.");
                                        job.ProgressStr = "Upload : child found.";
                                        var uploadeditem = children.Where(x => x.name.Contains(uploadfilename)).LastOrDefault();
                                        var remotehash = uploadeditem?.contentProperties?.md5;
                                        if (uphash != remotehash)
                                        {
                                            Config.Log.LogOut(string.Format("Upload : but hash not match. upload:{0} remote:{1}", uphash, remotehash));
                                            job.ProgressStr = "Upload : upload hash chech failed. remove item...";
                                            job.Progress = -1;
                                            checkretry = 0;
                                            DriveData.Drive.TrashItem(uploadeditem.id, ct).Wait(ct);
                                            Task.Delay(TimeSpan.FromSeconds(5), ct).Wait(ct);
                                        }
                                        done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                                        job.Progress = 1;
                                        throw new Exception("break");
                                    }
                                }, ct).Wait(ct);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                        if (checkretry > 0)
                            break;
                    }
                    if (retry == 0)
                    {
                        Config.Log.LogOut("Upload : failed.");
                        JobControler.ErrorOut("Upload : failed. {0}->{1} {2}", filename, uploadfilename, error_str);
                        job.Error("Upload failed.");
                        return;
                    }

                    if (Config.UseFilenameEncryption && Config.CryptMethod == CryptMethods.Method1_CTR)
                    {
                        Config.Log.LogOut("Encrypt Name.");
                        DriveData.EncryptFilename(uploadfilename: uploadfilename, enckey: enckey, checkpoint: checkpoint, ct: ct).ContinueWith((t) =>
                        {
                            if (!t.Result)
                            {
                                job.Error("Upload failed. filename encryption failed.");
                                JobControler.ErrorOut("Upload : failed. filename encryption failed. {0}", uploadfilename);
                            }
                        }, ct).Wait(ct);
                        done_files = DriveData.AmazonDriveTree[parent_id].children.Values.Select(x => x.info).ToArray();
                        if (job.IsError) return;
                    }

                    Config.Log.LogOut("Upload : done. "+uploadfilename);
                    job.ProgressStr = "Upload done.";
                    job.Progress = 1;
                    job.Result = 0;
                    Reload(parent_id);
                    return;
                });
            }
            return joblist.ToArray();
        }

        static public JobControler.Job[] downloadItems(IEnumerable<FileMetadata_Info> target, string downloadpath, JobControler.Job prevJob = null)
        {
            Config.Log.LogOut("Download Start.");
            target = target.SelectMany(x => DriveData.GetAllChildrenfromId(x.id));
            var itembasepath = FormMatch.GetBasePath(target.Select(x => DriveData.GetFullPathfromId(x.id)));
            target = target.Where(x => x.kind == "FILE");
            int f_all = target.Count();
            if (f_all == 0) return null;


            string savefilename = null;
            string savefilepath = null;

            if (f_all > 1)
            {
                savefilepath = downloadpath;
            }
            else
            {
                savefilename = downloadpath;
            }

            var joblist = new List<JobControler.Job>();
            foreach (var downitem in target)
            {
                var job = JobControler.CreateNewJob(JobControler.JobClass.Download, prevJob);
                job.WeekDepend = true;
                var filename = DriveData.AmazonDriveTree[downitem.id].DisplayName;
                job.DisplayName = filename;
                job.ProgressStr = "Wait for download";
                var ct = job.ct;
                joblist.Add(job);
                JobControler.Run(job, (j) =>
                {
                    if (indexpath != null)
                    {
                        job.ProgressStr = "wait for index remove";
                        while (Directory.EnumerateFileSystemEntries(indexpath).Count() > Config.ParallelUpload)
                        {
                            Task.Delay(5000, ct).Wait(ct);
                        }
                    }

                    Config.Log.LogOut("Download : " + filename);

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

                    job.Progress = 0;
                    job.ProgressStr = "Download...";


                    var targetfilename = savefilename;
                    if (savefilepath != null)
                    {
                        var itempath = DriveData.GetFullPathfromId(downitem.id).Substring(itembasepath.Length).Split('/');
                        var dpath = savefilepath;
                        foreach (var p in itempath.Take(itempath.Length - 1))
                        {
                            dpath = Path.Combine(dpath, p);
                            if (!Directory.Exists(dpath)) Directory.CreateDirectory(dpath);
                        }
                        targetfilename = Path.Combine(dpath, filename);
                    }

                    var retry = 5;
                    while (--retry > 0)
                    {
                        try
                        {
                            using (var outfile = File.Open(targetfilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                using (var ret = new AmazonDriveBaseStream(DriveData.Drive, downitem, parentJob: job))
                                using (var f = new PositionStream(ret, downitem.OrignalLength ?? 0))
                                {
                                    f.PosChangeEvent += (src, evnt) =>
                                    {
                                        job.Progress = (double)evnt.Position / evnt.Length;
                                        job.ProgressStr = evnt.Log;
                                    };
                                    ct.ThrowIfCancellationRequested();
                                    f.CopyToAsync(outfile, Config.DownloadBufferSize, ct).Wait(ct);
                                }
                            }
                            if (indexpath != null)
                            {
                                while (true)
                                {
                                    var indexfilename = Path.Combine(indexpath, Path.GetRandomFileName());
                                    if (File.Exists(indexfilename)) continue;
                                    using (var indexfile = File.Open(indexfilename, FileMode.Create, FileAccess.Write, FileShare.None))
                                    using (var sw = new StreamWriter(indexfile, Encoding.UTF8))
                                    {
                                        sw.Write(Path.GetFullPath(targetfilename));
                                        break;
                                    }
                                }
                            }
                            Config.Log.LogOut("Download : Done");
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("Download : Error " + ex.ToString());
                            JobControler.ErrorOut("Download : Error {0}\n{1}", filename, ex.ToString());
                            job.ProgressStr = "Error detected.";
                            job.Progress = double.NaN;
                            continue;
                        }
                    }
                    if (retry == 0)
                    {
                        job.Progress = double.NaN;
                        return;
                    }

                    job.ProgressStr = "done.";
                    job.Progress = 1;
                });
            }
            return joblist.ToArray();
        }
    }
}
