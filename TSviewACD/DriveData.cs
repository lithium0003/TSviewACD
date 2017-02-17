using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace TSviewACD
{
    class DriveData
    {
        public static AsyncLock DriveLock = new AsyncLock();

        public static AmazonDrive Drive = new AmazonDrive();

        static string root_id;
        static ConcurrentDictionary<string, ItemInfo> DriveTree = new ConcurrentDictionary<string, ItemInfo>();
        static List<FileMetadata_Info> historydata = new List<FileMetadata_Info>();
        static string checkpoint;

        [Serializable]
        [DataContract]
        public class RecordData
        {
            [DataMember]
            public string checkpoint;
            [DataMember]
            public ConcurrentDictionary<string, ItemInfo> DriveTree;

            public RecordData()
            {
                checkpoint = DriveData.checkpoint;
                DriveTree = DriveData.DriveTree;
            }
        }

        static public IDictionary<string, ItemInfo> AmazonDriveTree
        {
            get { return DriveTree; }
        }
        static public string AmazonDriveRootID
        {
            get { return root_id; }
        }
        static public string ChangeCheckpoint
        {
            get { return checkpoint; }
        }
        static public IEnumerable<FileMetadata_Info> AmazonDriveHistory
        {
            get { return historydata; }
        }
        static public async Task ChangeCryption1()
        {
            using (await DriveLock.LockAsync())
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(DriveTree.Values, (item) => item.ReloadCryptedMethod1());
                });
            }
        }
        static public async Task ChangeCryption2()
        {
            using (await DriveLock.LockAsync())
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(DriveTree.Values, (item) => item.ReloadCryptedMethod2());
                });
            }
        }


        public static bool SaveToBinaryFile(RecordData obj, string path)
        {
            try
            {
                using (var fs = new FileStream(path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (var ds = new GZipStream(fs, CompressionLevel.Fastest))
                using (var writer = XmlDictionaryWriter.CreateBinaryWriter(ds))
                {
                    var s = new DataContractSerializer(typeof(RecordData), new DataContractSerializerSettings() { MaxItemsInObjectGraph = int.MaxValue });
                    //シリアル化して書き込む
                    s.WriteObject(writer, obj);
                    writer.Flush();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static RecordData LoadFromBinaryFile(string path)
        {
            using (var fs = new FileStream(path,
                FileMode.Open,
                FileAccess.Read))
            using (var ds = new GZipStream(fs, CompressionMode.Decompress))
            using (var reader = XmlDictionaryReader.CreateBinaryReader(ds, new XmlDictionaryReaderQuotas()))
            {
                var s = new DataContractSerializer(typeof(RecordData), new DataContractSerializerSettings() { MaxItemsInObjectGraph = int.MaxValue });
                //読み込んで逆シリアル化する
                return (RecordData)s.ReadObject(reader);
            }
        }

        private static readonly string cachefile = Path.Combine(Config.Config_BasePath, "drivecache.bin");

        public delegate void DriveProgressHandler(string info);

        public static void RemoveCache()
        {
            while (true)
            {
                try
                {
                    File.Delete(cachefile);
                    //historydata.Clear();
                    DriveTree.Clear();
                    checkpoint = null;
                    root_id = null;
                    break;
                }
                catch
                {
                    continue;
                }
            }
        }

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
                    DriveTree.Clear();
                    RecordData cache = null;
                    await Task.Run(() =>
                    {
                        cache = LoadFromBinaryFile(cachefile);
                    }, ct);
                    inprogress?.Invoke("Restore tree...");
                    await Task.Run(() =>
                    {
                        ConstructDriveTree(cache);
                    }, ct);
                }
                await GetChanges(checkpoint: checkpoint, ct: ct, inprogress: inprogress, done: done);
                done?.Invoke("Changes Loaded.");
            }
            catch
            {
                // Load Root
                await GetChanges(ct: ct, inprogress: inprogress, done: done);
            }
        }

        public static async Task<FileMetadata_Info[]> GetChanges(
            string checkpoint = null,
            CancellationToken ct = default(CancellationToken),
            DriveProgressHandler inprogress = null,
            DriveProgressHandler done = null)
        {
            List<FileMetadata_Info> ret = new List<FileMetadata_Info>();
            inprogress?.Invoke("Loading Changes...");
            using (await DriveLock.LockAsync())
            {
                if (checkpoint == null)
                {
                    DriveTree.Clear();
                }
                historydata.Clear();
                bool updated = false;
                while (!ct.IsCancellationRequested)
                {
                    Changes_Info[] history = null;
                    int retry = 6;
                    while (--retry > 0)
                    {
                        try
                        {
                            history = await Drive.changes(checkpoint: checkpoint, ct: ct);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Config.Log.LogOut("[GetChanges] " + ex.Message);
                        }
                    }
                    foreach (var h in history)
                    {
                        if(!(h.end ?? false))
                        {
                            if (h.nodes.Count() > 0)
                            {
                                updated = true;
                                ConstructDriveTree(h.nodes);
                                ret.AddRange(h.nodes);
                                historydata.AddRange(h.nodes);
                            }
                            DriveData.checkpoint = h.checkpoint;
                            checkpoint = h.checkpoint;
                        }
                    }
                    if ((history.FirstOrDefault()?.nodes.Count() ?? 0) == 0 &&
                        (history.LastOrDefault()?.end ?? false))
                    {
                        break;
                    }
                }
                done?.Invoke("Changes Loaded.");
                if (updated)
                {
                    while (!ct.IsCancellationRequested)
                    {
                        if (await Task.Run(() =>
                        {
                            if (SaveToBinaryFile(new RecordData(), cachefile))
                                return true;
                            else
                                return false;
                        }, ct))
                        {
                            break;
                        }
                    }
                }
                return ret.ToArray();
            }
        }

        static private void ConstructDriveTree(IEnumerable<FileMetadata_Info> newdata)
        {
            foreach (var item in newdata)
                AddNewDriveItem(item);
        }

        static private void ConstructDriveTree(RecordData saveddata)
        {
            checkpoint = saveddata.checkpoint;
            DriveTree = saveddata.DriveTree;
            foreach (var key in saveddata.DriveTree.Keys)
            {
                var newdata = saveddata.DriveTree[key].info;
                if (newdata?.status == "AVAILABLE")
                {
                    foreach (var p in newdata.parents)
                    {
                        ItemInfo value;
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
                    DeleteDriveItem(newdata);
                }
            }
        }

        static private void AddNewDriveItem(FileMetadata_Info newdata)
        {
            ItemInfo value;
            if (newdata == null) return;
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
                                ItemInfo outvalue;
                                while (!value2.children.TryRemove(value.info.id, out outvalue))
                                    if (!value2.children.TryGetValue(value.info.id, out outvalue))
                                        break;
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
            else if (newdata.status == "TRASH" || newdata.status == "PURGED")
            {
                // deleted item
                DeleteDriveItem(newdata);
            }
        }

        static private void DeleteDriveItem(FileMetadata_Info deldata)
        {
            ItemInfo value;
            if (deldata == null) return;
            if (DriveTree.TryGetValue(deldata.id, out value))
            {
                var children = value.children.Values.ToArray();
                foreach (var child in children)
                {
                    DeleteDriveItem(child.info);
                }
                ItemInfo outitem;
                while(!DriveTree.TryRemove(deldata.id, out outitem))
                    if(!DriveTree.TryGetValue(deldata.id, out outitem))
                        break;
            }
            foreach (var p in deldata.parents)
            {
                if (DriveTree.TryGetValue(p, out value))
                {
                    ItemInfo outvalue;
                    while(!value.children.TryRemove(deldata.id, out outvalue))
                        if(!value.children.TryGetValue(deldata.id, out outvalue))
                            break;
                }
            }
        }

        public static string GetFullPathfromItem(ItemInfo info)
        {
            if (info.info.id == root_id) return "/";
            else
            {
                var parents = GetFullPathfromItem(DriveTree[info.info.parents[0]]);
                return parents + ((parents != "/") ? "/" : "") + info.DisplayName;
            }
        }

        public static string GetFullPathfromId(string id, bool nodecrypt = false)
        {
            if (id == root_id) return "/";
            else
            {
                var info = DriveTree[id];
                var parents = GetFullPathfromItem(DriveTree[info.info.parents[0]]);
                return parents + ((parents != "/") ? "/" : "") + ((nodecrypt)? info.info.name: info.DisplayName);
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
                var find_result = DriveTree[id].children.Where(x => x.Value.DisplayName == p);
                if (find_result.Count() == 0) break;
                id = find_result.First().Key;
            }
            return id;
        }

        public static async Task<bool> EncryptFilename(string uploadfilename, string enckey, string checkpoint, CancellationToken ct = default(CancellationToken))
        {
            int retry = 30;
            while (--retry > 0) {
                var child = (await GetChanges(checkpoint, ct).ConfigureAwait(false)).Where(x => x.name.Contains(uploadfilename)).LastOrDefault();
                if (child?.status == "AVAILABLE")
                {
                    Config.Log.LogOut("EncryptFilename");
                    using (var ms = new MemoryStream())
                    {
                        byte[] plain = Encoding.UTF8.GetBytes(enckey);
                        ms.Write(plain, 0, plain.Length);
                        ms.Position = 0;
                        using (var enc = new CryptCTR.AES256CTR_CryptStream(ms, child.id))
                        {
                            byte[] buf = new byte[ms.Length];
                            enc.Read(buf, 0, buf.Length);
                            string cryptname = "";
                            foreach (var c in buf)
                            {
                                cryptname += (char)('\u2800' + c);
                            }
                            var reItem = await Drive.renameItem(id: child.id, newname: cryptname, ct: ct).ConfigureAwait(false);
                            await GetChanges(checkpoint: ChangeCheckpoint, ct: ct).ConfigureAwait(false);
                            AmazonDriveTree[reItem.id].IsEncrypted = CryptMethods.Method1_CTR;
                            AmazonDriveTree[reItem.id].ReloadCryptedMethod1();
                            return true;
                        }
                    }
                }
                await Task.Delay(2000).ConfigureAwait(false);
            }
            return false;
        }

        public static string DecryptFilename(FileMetadata_Info downloaditem)
        {
            using (var ms = new MemoryStream())
            {
                string cryptname = downloaditem?.name;
                if (string.IsNullOrEmpty(cryptname)) return null;
                byte[] buf = new byte[cryptname.Length];
                int i = 0;
                foreach (var c in cryptname)
                {
                    if (c < '\u2800' || c > '\u28ff') return null;
                    buf[i++] = (byte)(c - '\u2800');
                }
                ms.Write(buf, 0, i);
                ms.Position = 0;
                using (var dec = new CryptCTR.AES256CTR_CryptStream(ms, downloaditem.id))
                {
                    byte[] plain = new byte[i];
                    dec.Read(plain, 0, plain.Length);
                    var plainname = Encoding.UTF8.GetString(plain);
                    if (plainname.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                    {
                        if (Regex.IsMatch(plainname ?? "", ".*?\\.[a-z0-9]{8}$"))
                            return plainname;
                        else
                            return null;
                    }
                    else
                        return null;
                }
            }
        }
    }

    public enum CryptMethods
    {
        Unknown,
        Method0_Plain,
        Method1_CTR,
        Method2_CBC_CarotDAV,
    }


    [Serializable]
    [DataContract]
    public class ItemInfo
    {
        [DataMember]
        public FileMetadata_Info info;
        [DataMember]
        [OptionalField(VersionAdded = 2)]
        public CryptMethods IsEncrypted;
        public bool CryptError = false;
        public TreeNode tree;
        public ConcurrentDictionary<string, ItemInfo> children = new ConcurrentDictionary<string, ItemInfo>();
        public string DisplayName
        {
            get
            {
                if (_DisplayName == null) ProcessCryption();
                return _DisplayName;
            }
        }

        private string _DisplayName;

        private void ProcessCryption()
        {
            if (IsEncrypted == CryptMethods.Unknown)
            {
                if (info?.name?.StartsWith(Config.CarotDAV_CryptNameHeader) ?? false)
                {
                    IsEncrypted = CryptMethods.Method2_CBC_CarotDAV;
                }
                else if (Regex.IsMatch(info?.name ?? "", ".*?\\.[a-z0-9]{8}\\.enc$"))
                {
                    IsEncrypted = CryptMethods.Method1_CTR;
                    _DisplayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(info.name));
                    return;
                }
                else if(Regex.IsMatch(info?.name ?? "", "^[\u2800-\u28ff]+$"))
                {
                    IsEncrypted = CryptMethods.Method1_CTR;
                    var decodename = DriveData.DecryptFilename(info);
                    if (decodename != null)
                    {
                        IsEncrypted = CryptMethods.Method1_CTR;
                        _DisplayName = Path.GetFileNameWithoutExtension(decodename);
                        return;
                    }
                    _DisplayName = info?.name;
                    CryptError = true;
                }
                else
                {
                    IsEncrypted = CryptMethods.Method0_Plain;
                }
            }
            switch (IsEncrypted)
            {
                case CryptMethods.Method0_Plain:
                    _DisplayName = info?.name;
                    break;
                case CryptMethods.Method1_CTR:
                    if (Regex.IsMatch(info?.name ?? "", ".*?\\.[a-z0-9]{8}\\.enc$"))
                    {
                        _DisplayName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(info.name));
                        CryptError = false;
                    }
                    else if (Regex.IsMatch(info?.name ?? "", "^[\u2800-\u28ff]+$"))
                    {
                        var decodename = DriveData.DecryptFilename(info);
                        if (decodename != null)
                        {
                            _DisplayName = Path.GetFileNameWithoutExtension(decodename);
                            CryptError = false;
                        }
                        else
                        {
                            _DisplayName = info?.name;
                            CryptError = true;
                        }
                    }
                    else
                    {
                        IsEncrypted = CryptMethods.Method0_Plain;
                        _DisplayName = info?.name;
                    }
                    break;
                case CryptMethods.Method2_CBC_CarotDAV:
                    {
                        var decodename = CryptCarotDAV.DecryptFilename(info?.name);
                        if (decodename != null)
                        {
                            _DisplayName = decodename;
                            CryptError = false;
                        }
                        else
                        {
                            _DisplayName = info?.name;
                            CryptError = true;
                        }
                    }
                    break;
            }
        }

        public ItemInfo(FileMetadata_Info thisdata)
        {
            info = thisdata;
        }

        public void ReloadCryptedMethod1()
        {
            if(IsEncrypted != CryptMethods.Method0_Plain)
            {
                _DisplayName = null;
                ProcessCryption();
            }
        }

        public void ReloadCryptedMethod2()
        {
            if (IsEncrypted == CryptMethods.Method0_Plain || IsEncrypted == CryptMethods.Method2_CBC_CarotDAV)
            {
                _DisplayName = null;
                IsEncrypted = CryptMethods.Unknown;
            }
            ProcessCryption();
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            children = new ConcurrentDictionary<string, ItemInfo>();
        }
    }


    public sealed class AsyncLock
    {
        private readonly System.Threading.SemaphoreSlim m_semaphore
          = new System.Threading.SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> m_releaser;

        public AsyncLock()
        {
            m_releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                    m_releaser :
                    wait.ContinueWith(
                      (_, state) => (IDisposable)state,
                      m_releaser.Result,
                      System.Threading.CancellationToken.None,
                      TaskContinuationOptions.ExecuteSynchronously,
                      TaskScheduler.Default
                    );
        }
        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
            public void Dispose() { m_toRelease.m_semaphore.Release(); }
        }
    }
}
