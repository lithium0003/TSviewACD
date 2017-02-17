using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
        static Dictionary<string, ItemInfo> DriveTree = new Dictionary<string, ItemInfo>();
        static List<FileMetadata_Info> historydata = new List<FileMetadata_Info>();
        static string checkpoint;

        [Serializable()]
        [DataContract]
        public class RecordData
        {
            [DataMember]
            public string checkpoint;
            [DataMember]
            public Dictionary<string, ItemInfo> DriveTree;

            public RecordData()
            {
                checkpoint = DriveData.checkpoint;
                DriveTree = DriveData.DriveTree;
            }
        }

        //static DriveData_Info AmazonDriveData
        //{
        //    get
        //    {
        //        var ret = new DriveData_Info();
        //        ret.checkpoint = checkpoint;
        //        ret.nodes = historydata.ToArray();
        //        return ret;
        //    }
        //}

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
                        catch (HttpRequestException ex)
                        {
                            Config.Log.LogOut("[GetChanges] " + ex.Message);
                        }
                    }
                    foreach (var h in history)
                    {
                        if(!(h.end ?? false))
                        {
                            if (h.nodes.Count() > 0) updated = true;
                            ConstructDriveTree(h.nodes);
                            ret.AddRange(h.nodes);
                            historydata.AddRange(h.nodes);
                            DriveData.checkpoint = h.checkpoint;
                        }
                    }
                    if (history.LastOrDefault()?.end ?? false) break;
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
            {
                AddNewDriveItem(item);
            }
        }

        static private void ConstructDriveTree(RecordData saveddata)
        {
            checkpoint = saveddata.checkpoint;
            DriveTree = saveddata.DriveTree;
            foreach (var key in saveddata.DriveTree.Keys)
            {
                var newdata = saveddata.DriveTree[key].info;
                if (newdata.status == "AVAILABLE")
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
                DeleteDriveItem(newdata);
            }
        }

        static private void DeleteDriveItem(FileMetadata_Info deldata)
        {
            ItemInfo value;
            if (DriveTree.TryGetValue(deldata.id, out value))
            {
                var children = value.children.Values.ToArray();
                foreach (var child in children)
                {
                    DeleteDriveItem(child.info);
                }
                DriveTree.Remove(deldata.id);
            }
            foreach (var p in deldata.parents)
            {
                if (DriveTree.TryGetValue(p, out value))
                {
                    value.children.Remove(deldata.id);
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
            var child = (await GetChanges(checkpoint, ct).ConfigureAwait(false)).Where(x => x.name.Contains(uploadfilename)).LastOrDefault();
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
                        await GetChanges(checkpoint: ChangeCheckpoint, ct: ct).ConfigureAwait(false);
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

    [Serializable()]
    [DataContract]
    public class ItemInfo
    {
        [DataMember]
        public FileMetadata_Info info;
        public TreeNode tree;
        public Dictionary<string, ItemInfo> children = new Dictionary<string, ItemInfo>();

        public ItemInfo(FileMetadata_Info thisdata)
        {
            info = thisdata;
        }

        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            children = new Dictionary<string, ItemInfo>();
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
