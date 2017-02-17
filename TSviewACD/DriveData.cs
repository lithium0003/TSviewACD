﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    class DriveData
    {
        public static AsyncLock DriveLock = new AsyncLock();

        public static AmazonDrive Drive = new AmazonDrive();

        static string root_id;
        static Dictionary<string, ItemInfo> DriveTree = new Dictionary<string, ItemInfo>();
        static List<Changes_Info> historydata = new List<Changes_Info>();

        static public IDictionary<string, ItemInfo> AmazonDriveTree
        {
            get { return DriveTree; }
        }
        static public IEnumerable<Changes_Info> AmazonDriveData
        {
            get { return historydata; }
        }
        static public string AmazonDriveRootID
        {
            get { return root_id; }
        }

        public static bool SaveToBinaryFile(object obj, string path)
        {
            try
            {
                using (var fs = new FileStream(path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (var ds = new GZipStream(fs, CompressionLevel.Optimal))
                {
                    var bf = new BinaryFormatter();
                    //シリアル化して書き込む
                    bf.Serialize(ds, obj);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object LoadFromBinaryFile(string path)
        {
            using (var fs = new FileStream(path,
                FileMode.Open,
                FileAccess.Read))
            using (var ds = new GZipStream(fs, CompressionMode.Decompress))
            {
                BinaryFormatter f = new BinaryFormatter();
                //読み込んで逆シリアル化する
                return f.Deserialize(ds);
            }
        }

        public const string cachefile = "drivecache.bin";

        public delegate void DriveProgressHandler(string info);

        public static async Task InitDrive(
            CancellationToken ct = default(CancellationToken),
            DriveProgressHandler inprogress = null,
            DriveProgressHandler done = null)
        {
            try
            {
                inprogress?.Invoke("Loading Cache...");
                using (await DriveLock.LockAsync())
                {
                    await Task.Run(() =>
                    {
                        historydata.Clear();
                        DriveTree.Clear();
                        historydata.AddRange((Changes_Info[])LoadFromBinaryFile(cachefile));
                        ConstructDriveTree(historydata);
                    }, ct);
                }
                string checkpoint = historydata.LastOrDefault()?.checkpoint;
                await GetChanges(checkpoint: checkpoint, ct: ct, inprogress: inprogress, done: done);
                done?.Invoke("Changes Loaded.");
            }
            catch
            {
                // Load Root
                await GetChanges(ct: ct, inprogress: inprogress, done: done);
            }
        }

        public static async Task<Changes_Info[]> GetChanges(
            string checkpoint = null,
            CancellationToken ct = default(CancellationToken),
            DriveProgressHandler inprogress = null,
            DriveProgressHandler done = null)
        {
            inprogress?.Invoke("Loading Changes...");
            using (await DriveLock.LockAsync())
            {
                if (checkpoint == null)
                {
                    DriveTree.Clear();
                    historydata.Clear();
                }
                while (!ct.IsCancellationRequested)
                {
                    var history = await Drive.changes(checkpoint: checkpoint, ct: ct);
                    ConstructDriveTree(history);
                    if (history.LastOrDefault()?.end ?? false)
                    {
                        historydata.AddRange(history.Where(x => !(x.end ?? false)));
                        break;
                    }
                    else
                        historydata.AddRange(history);
                }
                done?.Invoke("Changes Loaded.");
                while (!ct.IsCancellationRequested)
                {
                    if (SaveToBinaryFile(historydata.ToArray(), cachefile))
                        break;
                }
                if (checkpoint == null) return historydata.ToArray();
                return historydata.SkipWhile(x => x.checkpoint != checkpoint).SkipWhile(x => x.checkpoint == checkpoint).ToArray();
            }
        }

        static private void ConstructDriveTree(IEnumerable<Changes_Info> newdata)
        {
            foreach (var change in newdata)
            {
                if (change.end ?? false) return;
                foreach (var item in change.nodes)
                {
                    AddNewDriveItem(item);
                }
            }
        }

        static private void AddNewDriveItem(FileMetadata_Info newdata)
        {
            ItemInfo value;
            if (newdata.status == "AVAILABLE")
            {
                // exist item
                if (DriveTree.TryGetValue(newdata.id, out value))
                {
                    if(value.info != null)
                    {
                        foreach (var p in value.info.parents)
                        {
                            ItemInfo value2;
                            if (DriveTree.TryGetValue(p, out value2))
                            {
                                value2.children.Remove(value.info.id);
                            }
                        }
                    }
                    value.info = newdata;
                }
                else
                {
                    DriveTree[newdata.id] = new ItemInfo(newdata);
                }
                foreach (var p in newdata.parents)
                {
                    if (DriveTree.TryGetValue(p, out value))
                    {
                        value.children[newdata.id] = DriveTree[newdata.id];
                    }
                    else
                    {
                        DriveTree[p] = new ItemInfo(null);
                        DriveTree[p].children[newdata.id] = DriveTree[newdata.id];
                    }
                }
                if (newdata.isRoot ?? false)
                    root_id = newdata.id;
            }
            else
            {
                // deleted item
                if (DriveTree.TryGetValue(newdata.id, out value))
                {
                    DriveTree.Remove(newdata.id);
                    foreach (var p in newdata.parents)
                    {
                        if (DriveTree.TryGetValue(p, out value))
                        {
                            value.children.Remove(newdata.id);
                        }
                    }
                }
            }
        }

        public static string GetFullPathfromItem(ItemInfo info)
        {
            if (info.info.id == root_id) return "/";
            else
            {
                var parents = GetFullPathfromItem(DriveTree[info.info.parents[0]]);
                return parents + ((parents != "/") ? "/" : "") + info.info.name;
            }
        }

        public static string GetFullPathfromId(string id)
        {
            if (id == root_id) return "/";
            else
            {
                var info = DriveTree[id].info;
                var parents = GetFullPathfromItem(DriveTree[info.parents[0]]);
                return parents + ((parents != "/") ? "/" : "") + info.name;
            }
        }

        public static IEnumerable<FileMetadata_Info> GetAllChildrenfromId(string id)
        {
            List<FileMetadata_Info> ret = new List<FileMetadata_Info>();
            ret.Add(DriveTree[id].info);
            foreach (var child in DriveTree[id].children)
            {
                ret.AddRange(GetAllChildrenfromId(child.Key));
            }
            return ret;
        }

        public static string PathToID(string path_str)
        {
            string id = root_id;
            if (string.IsNullOrEmpty(path_str)) return id;
            var path = path_str.Split('/', '\\');
            foreach (var p in path)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var find_result = DriveTree[id].children.Where(x => x.Value.info.name == p);
                if (find_result.Count() == 0) break;
                id = find_result.First().Key;
            }
            return id;
        }

        public static async Task<bool> EncryptFilename(string uploadfilename, string enckey, string checkpoint, CancellationToken ct = default(CancellationToken))
        {
            var child = (await GetChanges(checkpoint, ct).ConfigureAwait(false)).SelectMany(x => x.nodes).Where(x => x.name.Contains(uploadfilename)).LastOrDefault();
            if (child?.status == "AVAILABLE")
            {
                Config.Log.LogOut("EncryptFilename");
                using (var ms = new MemoryStream())
                {
                    byte[] plain = Encoding.UTF8.GetBytes(enckey);
                    ms.Write(plain, 0, plain.Length);
                    ms.Position = 0;
                    using (var enc = new AES256CTR_CryptStream(ms, Config.DrivePassword, child.id))
                    {
                        byte[] buf = new byte[ms.Length];
                        enc.Read(buf, 0, buf.Length);
                        string cryptname = "";
                        foreach (var c in buf)
                        {
                            cryptname += (char)('\u2800' + c);
                        }
                        await Drive.renameItem(id: child.id, newname: cryptname, ct: ct).ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                        await GetChanges(historydata.LastOrDefault()?.checkpoint, ct).ConfigureAwait(false);
                        return true;
                    }
                }
            }
            else
                return false;
        }

        public static string DecryptFilename(FileMetadata_Info downloaditem)
        {
            Config.Log.LogOut("DecryptFilename");
            using (var ms = new MemoryStream())
            {
                string cryptname = downloaditem.name;
                byte[] buf = new byte[cryptname.Length];
                int i = 0;
                foreach (var c in cryptname)
                {
                    if (c < '\u2800' || c > '\u28ff') return null;
                    buf[i++] = (byte)(c - '\u2800');
                }
                ms.Write(buf, 0, i);
                ms.Position = 0;
                using (var dec = new AES256CTR_CryptStream(ms, Config.DrivePassword, downloaditem.id))
                {
                    byte[] plain = new byte[i];
                    dec.Read(plain, 0, plain.Length);
                    return Encoding.UTF8.GetString(plain);
                }
            }
        }
    }
}
