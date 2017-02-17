using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    class AmazonDriveStreamConfig
    {
        public const int slotsize = 2 * 1024 * 1024;
        public const int slotbacklog = 64;
        public const int lockslotfirstnum = 4;
        public const int lockslotlastnum = 4;
        public const int preforwardnum = 10;
        public const int slotnearby = 3;
        public const int slotkeepold = slotbacklog / 2;
        public const int shortbuflen = slotsize;
        public const int extraslot = 10;
    }

    public interface IHashStream
    {
        string Hash
        {
            get;
        }
    }

    public class AmazonDriveHashException : Exception
    {
    }

    class NullStream : Stream
    {
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return 0; } }
        public override long Position
        {
            get { return 0; }
            set { }
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count) { }
    }

    class HashStream : Stream, IHashStream
    {
        Stream innerStream;
        HashAlgorithm hasher;
        bool Invalid = false;
        long pos;
        bool lengthSeek = false;

        public string Hash
        {
            get
            {
                Flush();
                return (Invalid) ? "" : BitConverter.ToString(hasher.Hash).Replace("-", "");
            }
        }

        public HashStream(Stream s, HashAlgorithm hash) : base()
        {
            innerStream = s;
            try
            {
                pos = innerStream.Position;
            }
            catch
            {
                pos = 0;
            }
            hasher = hash;
        }

        public HashStream(HashAlgorithm hash) : base()
        {
            pos = 0;
            hasher = hash;
        }

        public override long Length
        {
            get
            {
                if (innerStream == null) throw new NotImplementedException();
                return innerStream.Length;
            }
        }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override void Flush()
        {
            hasher.TransformFinalBlock(new byte[0], 0, 0);
        }

        public override long Position
        {
            get
            {
                return pos;
            }
            set
            {
                if (pos != value) Invalid = true;
                if (innerStream != null) innerStream.Position = value;
                else throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (innerStream == null) return -1;
            int ret = innerStream.Read(buffer, offset, count);
            if (!lengthSeek && !Invalid)
                hasher.TransformBlock(buffer, offset, ret, buffer, offset);
            pos += ret;
            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset == 0 && origin == SeekOrigin.Current) return pos;
            if (pos == 0 && offset == 0 && origin == SeekOrigin.Begin)
            {
                lengthSeek = false;
            }
            else if (pos == 0 && offset == 0 && origin == SeekOrigin.End)
            {
                lengthSeek = true;
            }
            else
            {
                Invalid = true;
            }
            if (innerStream != null)
                return innerStream.Seek(offset, origin);
            else
                throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (lengthSeek || Invalid) return;
            hasher.TransformBlock(buffer, offset, count, buffer, offset);
            pos += count;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }


    class MemoryStreamSlot : IDisposable
    {
        bool disposed = false;

        long _offset;
        long _length;
        DateTime starttime = DateTime.Now;
        DateTime readtime = default(DateTime);

        public Stream Stream;

        public long Offset
        {
            get { return _offset; }
        }
        public long Length
        {
            get { return _length; }
        }

        public TimeSpan Age
        {
            get { return DateTime.Now - starttime; }
        }

        public void TouchToRead()
        {
            readtime = DateTime.Now;
        }

        public TimeSpan ReadAge
        {
            get { return DateTime.Now - readtime; }
        }

        public MemoryStreamSlot()
        {
            _offset = 0;
        }

        public MemoryStreamSlot(Stream stream, long offset)
        {
            _offset = offset;
            _length = stream.Length;
            Stream = stream;
            //Config.Log.LogOut(string.Format("AmazonDriveStream : add {0:#,0} - {1:#,0}", _offset, _offset + _length - 1));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!disposed)
            {
                if (isDisposing)
                {
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : remove {0:#,0} - {1:#,0}", _offset, _offset + _length - 1));
                    Stream?.Dispose();
                    Stream = null;
                }
                disposed = true;
            }
        }

        ~MemoryStreamSlot()
        {
            Dispose(false);
        }
    }

    class SlotTask : IDisposable
    {
        bool disposed = false;

        int failcount = 0;

        AmazonDrive Drive;
        FileMetadata_Info targetItem;

        CancellationTokenSource Internal_cts = new CancellationTokenSource();
        CancellationTokenSource cts;

        long lastslot;

        long readslot = -1;
        bool done = false;

        public bool leadThread = false;

        public long ReadingSlotno
        {
            get { return readslot; }
        }

        public bool Done
        {
            get { return done; }
        }

        public SlotTask(AmazonDrive Drive, FileMetadata_Info targetItem, CancellationToken ct = default(CancellationToken))
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct, Internal_cts.Token);
            this.Drive = Drive;
            this.targetItem = targetItem;
            lastslot = ((targetItem.OrignalLength ?? 0) - 1) / AmazonDriveStreamConfig.slotsize;
        }

        public void StartDownload(long slotno, ConcurrentDictionary<long, MemoryStreamSlot> slot, BlockingCollection<KeyValuePair<long, MemoryStreamSlot>> SlotBuffer)
        {
            int timeout = (int)(1000 * (double)AmazonDriveStreamConfig.shortbuflen / (Config.FFmodule_TransferLimit * 1024));
            long start = slotno * AmazonDriveStreamConfig.slotsize;
            long length = AmazonDriveStreamConfig.slotsize;
            if (start + length > (targetItem.OrignalLength ?? 0))
            {
                length = (targetItem.OrignalLength ?? 0) - start;
            }
            if (length <= 0) return;
            readslot = slotno;
            done = false;
            CancellationTokenSource cancel_cts = new CancellationTokenSource();
            var cts_1 = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancel_cts.Token);

            //Config.Log.LogOut(string.Format("AmazonDriveStream : start to download slot {0} offset {1:#,0} - ", slotno, start));

            Drive.downloadFile(targetItem, start, ct: cts_1.Token)
            .ContinueWith(task =>
            {
                if (!task.Wait(timeout, cts_1.Token))
                {
                    cts_1.Token.ThrowIfCancellationRequested();
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : wait timeout slot {0}", slotno));
                    throw new IOException("transfer timeout0");
                }
                using (var stream = task.Result)
                {
                    while (slotno <= lastslot)
                    {
                        //Config.Log.LogOut(string.Format("AmazonDriveStream : download slot {0}", slotno));
                        cts_1.Token.ThrowIfCancellationRequested();

                        // すでに取得済みかチェック
                        MemoryStreamSlot o;
                        if (slot.TryGetValue(slotno, out o))
                        {
                            cts.Cancel();
                        }

                        readslot = slotno;
                        byte[] buffer = new byte[(length > AmazonDriveStreamConfig.shortbuflen) ? AmazonDriveStreamConfig.shortbuflen : length];
                        var mem = new MemoryStream();
                        var stime = DateTime.Now;
                        int loopdelay = 0;
                        while (length > 0)
                        {
                            cts_1.Token.ThrowIfCancellationRequested();
                            var tret = stream.ReadAsync(buffer, 0, buffer.Length).ContinueWith(task2 =>
                            {
                                if (!task2.Wait(timeout, cts_1.Token))
                                {
                                    cts_1.Token.ThrowIfCancellationRequested();
                                    //Config.Log.LogOut(string.Format("AmazonDriveStream : wait timeout slot {0}", slotno));
                                    throw new IOException("transfer timeout1");
                                }
                                var ret = task2.Result;
                                mem.Write(buffer, 0, ret);
                                length -= ret;
                                loopdelay += 5;
                                if ((DateTime.Now - stime).TotalMilliseconds > timeout + loopdelay)
                                {
                                    //Config.Log.LogOut(string.Format("AmazonDriveStream : transfer timeout slot {0}", slotno));
                                    throw new IOException("transfer timeout2");
                                }
                                if (buffer.Length > length)
                                    buffer = new byte[length];
                            }, cts_1.Token, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default)
                            .Wait(timeout, cts_1.Token);
                            if (!tret)
                            {
                                //Config.Log.LogOut(string.Format("AmazonDriveStream : wait2 timeout slot {0}", slotno));
                                throw new IOException("transfer timeout3");
                            }
                        }
                        cts_1.Token.ThrowIfCancellationRequested();
                        var newslot = new MemoryStreamSlot(mem, start);
                        if (leadThread)
                        {
                            while (leadThread)
                            {
                                if (SlotBuffer.TryAdd(new KeyValuePair<long, MemoryStreamSlot>(slotno, newslot), 500, cts_1.Token))
                                    break;
                            }
                            if (!leadThread)
                            {
                                if (slot.GetOrAdd(slotno, newslot) != newslot)
                                    cts.Cancel();
                            }
                        }
                        else
                        {
                            if (slot.GetOrAdd(slotno, newslot) != newslot)
                                cts.Cancel();
                        }
                        cts_1.Token.ThrowIfCancellationRequested();

                        start = ++slotno * AmazonDriveStreamConfig.slotsize;
                        length = AmazonDriveStreamConfig.slotsize;
                        if (start + length > (targetItem.OrignalLength ?? 0))
                        {
                            length = (targetItem.OrignalLength ?? 0) - start;
                        }
                        if (length <= 0)
                        {
                            return;
                        }
                    }
                }
            }, cts_1.Token, TaskContinuationOptions.LongRunning, TaskScheduler.Default)
            .ContinueWith(task =>
            {
                leadThread = false;
                if (task.IsFaulted)
                {
                    var e = task.Exception;
                    e.Flatten().Handle(ex =>
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveStream : ERROR {0}", ex.Message));
                        return true;
                    });
                    e.Handle(ex =>
                    {
                        return true;
                    });
                    if (++failcount < 10)
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveStream : ERROR restart to download {0} - ", slotno));
                        cancel_cts.Cancel(true);
                        StartDownload(slotno, slot, SlotBuffer);
                    }
                    else
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveStream : ERROR too much fail stop. {0}", slotno));
                        done = true;
                    }
                }
                else if (task.IsCanceled)
                {
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : cancel to download {0}", slotno));
                    done = true;
                }
                else if (task.IsCompleted)
                {
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : finish download {0}", slotno));
                    done = true;
                }
                cancel_cts.Cancel(true);
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!disposed)
            {
                if (isDisposing)
                {
                    Internal_cts.Cancel(true);
                }
                disposed = true;
            }
        }

        ~SlotTask()
        {
            Dispose(false);
        }
    }

    class SlotMaster : IDisposable
    {
        bool disposed = false;

        AmazonDrive Drive;
        FileMetadata_Info targetItem;
        ConcurrentDictionary<long, DateTime> accesslog = new ConcurrentDictionary<long, DateTime>();
        ConcurrentDictionary<long, MemoryStreamSlot> slot = new ConcurrentDictionary<long, MemoryStreamSlot>();
        BlockingCollection<SlotTask> Tasks = new BlockingCollection<SlotTask>();
        BlockingCollection<KeyValuePair<long, MemoryStreamSlot>> SlotBuffer = new BlockingCollection<KeyValuePair<long, MemoryStreamSlot>>(AmazonDriveStreamConfig.extraslot);
        CancellationTokenSource cts_internal = new CancellationTokenSource();
        CancellationTokenSource cts;

        long lastslot;
        long lockslot1;
        long lockslot2;

        long? StartLock = null;
        long? EndLock = null;

        public void LockRange(long start, long end)
        {
            StartLock = start;
            EndLock = end;
        }

        public void ReleaseLockRange()
        {
            StartLock = null;
            EndLock = null;
        }

        public int ThreadCount
        {
            get { return Tasks.Count; }
        }

        public SlotMaster(AmazonDrive Drive, FileMetadata_Info downitem, CancellationToken ct = default(CancellationToken))
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cts_internal.Token, ct);
            this.Drive = Drive;
            targetItem = downitem;
            lastslot = ((downitem.OrignalLength ?? 0) - 1) / AmazonDriveStreamConfig.slotsize;
            lockslot1 = AmazonDriveStreamConfig.lockslotfirstnum;
            lockslot2 = lastslot - AmazonDriveStreamConfig.lockslotlastnum;
            if (lockslot2 < lockslot1)
            {
                lockslot2 = lockslot1;
            }
            int extraslot = 0;
            Task.Run(() =>
            {
                foreach (var newitem in SlotBuffer.GetConsumingEnumerable(cts.Token))
                {
                    try
                    {
                        slot.GetOrAdd(newitem.Key, newitem.Value);

                        while (slot.Count > AmazonDriveStreamConfig.slotbacklog + extraslot)
                            Task.Delay(100, cts.Token).Wait(cts.Token);
                        if (cts.Token.IsCancellationRequested) return;
                    }
                    catch { }
                }
            }, cts.Token);
            Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        const int slotnumc = AmazonDriveStreamConfig.slotbacklog;
                        // slotが多すぎるのでいらないものから消す
                        if (slot.Count > slotnumc)
                        {
                            var pos = slot
                            .OrderByDescending(x => x.Value.Age)
                            .OrderBy(x => x.Value.ReadAge)
                            .First().Key;

                            var s = StartLock;
                            if (s != null)
                                pos = s.Value;

                            //Config.Log.LogOut(string.Format("AmazonDriveStream : Removing slots current pos {0}", pos));

                            var deleteitem = slot
                            .Where(x => !(x.Key >= StartLock && x.Key <= EndLock))
                            .Where(x => x.Key > lockslot1 && x.Key < lockslot2);

                            deleteitem = deleteitem
                            .Where(x => x.Key < pos - AmazonDriveStreamConfig.slotkeepold || x.Key > pos + AmazonDriveStreamConfig.slotbacklog * 2)
                            .OrderByDescending(x => x.Value.ReadAge)
                            .Take(slot.Count - slotnumc).ToArray();
                            foreach (var item in deleteitem)
                            {
                                MemoryStreamSlot o;
                                if (slot.TryRemove(item.Key, out o))
                                {
                                    //Config.Log.LogOut(string.Format("AmazonDriveStream : Remove slot {0} pos {1:#,0} len {2:#,0}", item.Key, o.Offset, o.Length));
                                    if (!(item.Key >= StartLock && item.Key <= EndLock))
                                        o.Dispose();
                                    else
                                        slot.GetOrAdd(item.Key, o);
                                }
                            }
                            extraslot = slot.Count - slotnumc;
                        }
                        // 終了したタスクを除去する
                        if (Tasks.Any(x => x.Done))
                        {
                            var deleteitem = Tasks.Where(x => x.Done).ToArray();
                            foreach (var item in deleteitem)
                            {
                                SlotTask o;
                                if (Tasks.TryTake(out o))
                                {
                                    //Config.Log.LogOut(string.Format("AmazonDriveStream : Remove end Task slot {0}", o.ReadingSlotno));
                                    o.Dispose();
                                }
                            }
                        }
                        // 走りすぎているスレットを消す
                        if (accesslog.Count() > 0)
                        {
                            var min_point = accesslog.OrderByDescending(x => x.Value).Take(1).Min(x => x.Key);
                            var max_point = min_point + AmazonDriveStreamConfig.slotbacklog;
                            min_point = Math.Max(min_point - AmazonDriveStreamConfig.slotnearby * 2, 0);
                            max_point = Math.Min(max_point + AmazonDriveStreamConfig.slotnearby * 2, lastslot);
                            //Config.Log.LogOut(string.Format("AmazonDriveStream : min_point {0}", min_point));
                            //Config.Log.LogOut(string.Format("AmazonDriveStream : max_point {0}", max_point));
                            if (min_point < lockslot2 && Tasks.Any(x => x.ReadingSlotno < min_point && x.ReadingSlotno > lockslot1))
                            {
                                var deleteitem = Tasks.Where(x => x.ReadingSlotno < min_point && x.ReadingSlotno > lockslot1).ToList();
                                SlotTask o;
                                while (deleteitem.Count > 0 && Tasks.TryTake(out o))
                                {
                                    if (deleteitem.Contains(o))
                                    {
                                        //Config.Log.LogOut(string.Format("AmazonDriveStream : Remove1 Task slot {0} too far({1})", o.ReadingSlotno, min_point));
                                        deleteitem.Remove(o);
                                        o.Dispose();
                                    }
                                    else
                                    {
                                        Tasks.Add(o);
                                    }
                                }
                            }
                            if (Tasks.Count == 1)
                            {
                                Tasks.First().leadThread = true;
                            }
                            else
                            {
                                foreach (var item in Tasks)
                                {
                                    item.leadThread = false;
                                }
                            }
                            if (max_point > lockslot1 && Tasks.Any(x => x.ReadingSlotno > max_point && x.ReadingSlotno < lockslot2))
                            {
                                var deleteitem = Tasks.Where(x => x.ReadingSlotno > max_point && x.ReadingSlotno < lockslot2).ToList();
                                SlotTask o;
                                while (deleteitem.Count > 0 && Tasks.TryTake(out o))
                                {
                                    if (deleteitem.Contains(o))
                                    {
                                        if (o.leadThread)
                                        {
                                            //Config.Log.LogOut(string.Format("AmazonDriveStream : LeadThread Task slot {0} too far({1})", o.ReadingSlotno, min_point));
                                            deleteitem.Remove(o);
                                            Tasks.Add(o);
                                        }
                                        else
                                        {
                                            //Config.Log.LogOut(string.Format("AmazonDriveStream : Remove2 Task slot {0} too far({1})", o.ReadingSlotno, min_point));
                                            deleteitem.Remove(o);
                                            o.Dispose();
                                        }
                                    }
                                    else
                                    {
                                        Tasks.Add(o);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : Tasks {0} slots {1}", Tasks.Count, slot.Count));
                    //Config.Log.LogOut(string.Format("AmazonDriveStream : slot {0}", string.Join(",",Tasks.Select(x=>x.ReadingSlotno.ToString()))));
                    Task.Delay(500, cts.Token).Wait(cts.Token);
                }
            }, cts.Token);
        }

        public void TouchSlot(long slotno)
        {
            accesslog[slotno] = DateTime.Now;
        }

        public void CreateTask(long Slotno)
        {
            if (Slotno < 0 || Slotno > lastslot) return;
            // すでに取得済みかチェック
            // 取得していない最後尾を探す
            long org_slotno = Slotno;
            MemoryStreamSlot o;
            while (slot.TryGetValue(Slotno, out o) && ++Slotno <= lastslot)
                ;
            if (Slotno > lastslot) return;

            // 十分取得しているので新たに起動しない
            if (Slotno - org_slotno >= (AmazonDriveStreamConfig.slotbacklog - AmazonDriveStreamConfig.slotkeepold) / 2 && Tasks.Count > 0) return;

            // 近くのスロットまで読みに来ているかチェック
            var slotnos = Tasks.Select(x => x.ReadingSlotno).ToArray();
            foreach (var sno in slotnos)
            {
                if (sno >= Slotno - AmazonDriveStreamConfig.slotnearby && sno <= Slotno + AmazonDriveStreamConfig.extraslot)
                {
                    return;
                }
            }
            //Config.Log.LogOut(string.Format("AmazonDriveStream : CreateTask {0}->{1}", org_slotno, Slotno));
            //Config.Log.LogOut(string.Format("AmazonDriveStream : CreateTask sno {0}", string.Join(",", slotnos.Select(x=>x.ToString()))));

            var t = new SlotTask(Drive, targetItem, cts.Token);
            Tasks.Add(t);
            t.StartDownload(Slotno, slot, SlotBuffer);
        }

        public bool TryGetSlot(long slotno, out MemoryStreamSlot mem)
        {
            mem = null;
            if (slotno < 0 || slotno > lastslot) return false;
            TouchSlot(slotno);
            return slot.TryGetValue(slotno, out mem);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (!disposed)
            {
                if (isDisposing)
                {
                    cts.Cancel();
                    if (Tasks != null)
                    {
                        SlotTask o;
                        while (Tasks.TryTake(out o))
                            o.Dispose();
                        Tasks.Dispose();
                        Tasks = null;
                    }
                    if (SlotBuffer != null)
                    {
                        KeyValuePair<long, MemoryStreamSlot> o2;
                        while (SlotBuffer.TryTake(out o2))
                            o2.Value.Dispose();
                        SlotBuffer.Dispose();
                        SlotBuffer = null;
                    }
                    if (slot != null)
                    {
                        foreach (var key in slot.Keys.ToArray())
                        {
                            slot[key].Dispose();
                        }
                        slot = null;
                    }
                    accesslog = null;
                }
                disposed = true;
            }
        }

        ~SlotMaster()
        {
            Dispose(false);
        }
    }

    class AmazonDriveSeekableStream : Stream
    {
        long FileSize;
        long _Position;
        AmazonDrive Drive;
        FileMetadata_Info targetItem;
        string OrgFilename = null;
        int _ReadTimeout = -1;
        SlotMaster slots;
        CancellationTokenSource cts = new CancellationTokenSource();

        long lastslot;
        long lockslot1;
        long lockslot2;

        public AmazonDriveSeekableStream(AmazonDrive Drive, FileMetadata_Info downitem) : base()
        {
            this.Drive = Drive;
            targetItem = downitem;
            FileSize = targetItem.OrignalLength ?? 0;

            if (FileSize <= 0) return;

            slots = new SlotMaster(Drive, downitem, cts.Token);

            _Position = 0;

            if (downitem.contentProperties?.size > ConfigAPI.FilenameChangeTrickSize && !Regex.IsMatch(downitem.name, "^[\x20-\x7e]*$"))
            {
                OrgFilename = downitem.name;
                Config.Log.LogOut("AmazonDriveSeekableStream : <BIG FILE> temporary filename change");
                Config.Log.LogOut("AmazonDriveSeekableStream : orgnal name : " + OrgFilename);
                Drive.renameItem(downitem.id, ConfigAPI.temporaryFilename + downitem.id).Wait();
            }

            lastslot = ((downitem.OrignalLength ?? 0) - 1) / AmazonDriveStreamConfig.slotsize;
            lockslot1 = AmazonDriveStreamConfig.lockslotfirstnum;
            lockslot2 = lastslot - AmazonDriveStreamConfig.lockslotlastnum;
            if (lockslot2 < lockslot1)
            {
                lockslot2 = lockslot1;
            }
            slots.CreateTask(0);
            slots.CreateTask(lockslot2);
            slots.CreateTask(lockslot1);
            slots.CreateTask(lockslot1 + AmazonDriveStreamConfig.preforwardnum);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                slots?.Dispose();
            }

            if (OrgFilename != null)
            {
                Config.Log.LogOut("AmazonDriveSeekableStream : rename to orignal");
                Drive.renameItem(targetItem.id, OrgFilename).Wait();
                OrgFilename = null;
            }

            base.Dispose(disposing);
        }

        public override long Length { get { return FileSize; } }
        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanSeek { get { return true; } }
        public override void Flush() { /* do nothing */ }
        public override int ReadTimeout
        {
            get
            {
                return _ReadTimeout;
            }

            set
            {
                _ReadTimeout = value;
                if (value == 0)
                {
                    cts.Cancel();
                }
            }
        }

        public override long Position
        {
            get
            {
                return _Position;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //Config.Log.LogOut(string.Format("AmazonDriveStream : read pos {0:#,0} len {1:#,0}", _Position, count));
            if (_Position + count > FileSize) count = (int)(FileSize - _Position);
            if (!EnsurePosition(_Position, count))
                return 0;
            long p_old = _Position;
            int c_old = count;
            try
            {
                var ret = 0;
                var LastOffset = _Position + count - 1;
                var s = _Position / AmazonDriveStreamConfig.slotsize;
                var e = LastOffset / AmazonDriveStreamConfig.slotsize;
                MemoryStreamSlot mem;
                while (slots.TryGetSlot(s, out mem) && s++ <= e)
                {
                    mem.Stream.Position = _Position - mem.Offset;
                    var len = (int)(mem.Stream.Length - mem.Stream.Position);
                    if (len > count) len = count;
                    var ret1 = mem.Stream.Read(buffer, offset, len);
                    _Position += ret1;
                    ret += ret1;
                    if (ret1 < len)
                        return ret;
                    count -= ret1;
                    offset += ret1;
                    if (count <= 0)
                        return ret;
                }
                return ret;
            }
            finally
            {
                slots.ReleaseLockRange();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newOffset = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newOffset = offset;
                    break;
                case SeekOrigin.Current:
                    newOffset = Position + offset;
                    break;
                case SeekOrigin.End:
                    newOffset = FileSize - offset;
                    break;
            }
            _Position = newOffset;
            var s = newOffset / AmazonDriveStreamConfig.slotsize - 1;
            s = (s < 0) ? 0 : (s > lastslot) ? lastslot : s;
            //Config.Log.LogOut(string.Format("AmazonDriveStream : seek pos {0:#,0} slot {1}", _Position, s));
            slots.TouchSlot(s);
            slots.CreateTask(s);
            slots.CreateTask(s + AmazonDriveStreamConfig.preforwardnum);
            slots.CreateTask(s + AmazonDriveStreamConfig.preforwardnum * 2);
            return Position;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // do nothing
        }

        public override void SetLength(long value)
        {
            // do nothing
        }

        private bool EnsurePosition(long Offset, int length)
        {
            if (Offset < 0 || Offset + length > FileSize) return false;
            var LastOffset = Offset + length;
            var s = Offset / AmazonDriveStreamConfig.slotsize;
            var e = LastOffset / AmazonDriveStreamConfig.slotsize;

            slots.LockRange(s, e);
            var stime = DateTime.Now;

            //Config.Log.LogOut(string.Format("AmazonDriveStream : Ensure pos {0:#,0}({2}) end {1:#,0}({3})", Offset, LastOffset, s, e));
            slots.CreateTask(s);

            try
            {
                while (s <= e)
                {
                    int count = 0;
                    MemoryStreamSlot o;
                    while (!slots.TryGetSlot(s, out o))
                    {
                        if ((DateTime.Now - stime).TotalSeconds > 120)
                        {
                            Config.Log.LogOut(string.Format("AmazonDriveStream : ERROR timeout Ensure pos {0:#,0}({2}) end {1:#,0}({3})", Offset, LastOffset, s, e));
                            return false;
                        }
                        if (slots.ThreadCount < 1 || ++count > 5)
                        {
                            count = 0;
                            slots.CreateTask(s);
                        }
                        Task.Delay(100, cts.Token).Wait(cts.Token);
                    }
                    s++;
                    o.TouchToRead();
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            return true;
        }
    }

    class AmazonDriveBaseStream : Stream
    {
        AmazonDrive Drive;
        FileMetadata_Info targetItem;
        long FileSize;
        long pos = 0;
        string OrgFilename = null;
        Stream innerStream;
        int failcount = 0;
        CancellationToken ct;
        bool autodecrypt;
        bool EOF = false;
        bool hashcheck = false;
        JobControler.Job downloadJob;

        protected void InitStream()
        {
            Drive.downloadFile(targetItem, ct: ct, autodecrypt: autodecrypt).ContinueWith(x =>
            {
                innerStream = x.Result;
                EOF = false;
                pos = 0;
            }, ct)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var e = task.Exception;
                    e.Flatten().Handle(ex =>
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveBaseStream : ERROR {0}", ex.Message));
                        return true;
                    });
                    e.Handle(ex =>
                    {
                        return true;
                    });
                    if (++failcount < 10)
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveBaseStream : ERROR restart to download"));
                        InitStream();
                    }
                    else
                    {
                        Config.Log.LogOut(string.Format("AmazonDriveBaseStream : ERROR too much fail stop."));
                    }
                }
            })
            .Wait(ct);
        }

        public override bool CanRead
        {
            get
            {
                return innerStream?.CanRead ?? false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return FileSize;
            }
        }

        public override long Position
        {
            get
            {
                return pos;
            }

            set
            {
                if (innerStream != null) innerStream.Position = value;
            }
        }

        public AmazonDriveBaseStream(AmazonDrive Drive, FileMetadata_Info downitem, bool autodecrypt = true, JobControler.Job parentJob = null) : base()
        {
            this.Drive = Drive;
            targetItem = downitem;
            FileSize = targetItem.OrignalLength ?? 0;
            this.autodecrypt = autodecrypt;
            if (parentJob == null)
            {
                downloadJob = JobControler.CreateNewJob(JobControler.JobClass.Download);
                downloadJob.DisplayName = downitem.name;
                downloadJob.ProgressStr = "wait for download";
                downloadJob.Progress = -1;
                ct = downloadJob.ct;
            }
            else
            {
                ct = parentJob.ct;
            }

            if (FileSize < 0) return;

            if (downitem.contentProperties?.size > ConfigAPI.FilenameChangeTrickSize && !Regex.IsMatch(downitem.name, "^[\x20-\x7e]*$"))
            {
                Interlocked.Increment(ref Config.AmazonDriveTempCount);
                OrgFilename = targetItem.name;
                Config.Log.LogOut("AmazonDriveBaseStream : <BIG FILE> temporary filename change");
                Config.Log.LogOut("AmazonDriveBaseStream : orgnal name : " + OrgFilename);
                Drive.renameItem(targetItem.id, ConfigAPI.temporaryFilename + targetItem.id).Wait();
            }

            InitStream();
            if(parentJob == null)
            {
                JobControler.Run(downloadJob, (j) =>
                {
                    downloadJob.ProgressStr = "download...";
                    downloadJob.Wait(ct: ct);
                    downloadJob.Progress = 1;
                    downloadJob.ProgressStr = "done.";
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                downloadJob?.Cancel();
                innerStream?.Dispose();
            }

            if (OrgFilename != null)
            {
                Config.Log.LogOut("AmazonDriveBaseStream : rename to orignal");
                Drive.renameItem(targetItem.id, OrgFilename).Wait();
                OrgFilename = null;
                Interlocked.Decrement(ref Config.AmazonDriveTempCount);
            }

            base.Dispose(disposing);
        }

        public override void Flush()
        {
            innerStream?.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            //Config.Log.LogOut(string.Format("AmazonDriveBaseStream : Seek {0} {1}", offset, origin));
            if (origin == SeekOrigin.Begin && offset == 0)
            {
                if(innerStream?.Position > 0)
                {
                    InitStream();
                }
            }
            var ret = innerStream?.Seek(offset, origin) ?? 0;
            pos = ret;
            return ret;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //Config.Log.LogOut(string.Format("AmazonDriveBaseStream : read pos:{0} count:{1}", pos, count));
            ct.ThrowIfCancellationRequested();
            var ret = innerStream?.Read(buffer, offset, count) ?? -1;
            pos += ret;
            if (pos >= FileSize) EOF = true;
            if (EOF && !hashcheck)
            {
                if ((innerStream as IHashStream).Hash.ToLower() != targetItem.contentProperties?.md5)
                {
                    Config.Log.LogOut(string.Format("AmazonDriveBaseStream : MD5 ERROR(Server:{0}, download:{1})",
                        targetItem.contentProperties?.md5,
                        (innerStream as IHashStream).Hash.ToLower()
                        ));
                    throw new AmazonDriveHashException();
                }
                else
                {
                    Config.Log.LogOut("AmazonDriveBaseStream : MD5 OK");
                }
                hashcheck = true;
            }
            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

