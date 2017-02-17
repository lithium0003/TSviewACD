using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSviewACD
{
    /// <summary>
    /// 読み書きの速度を制限するストリーム
    /// </summary>
    class ThrottleUploadStream : ThrottleStream
    {
        public ThrottleUploadStream(Stream s) : base(s, double.NaN)
        {
        }

        protected override double TargetBandwidth
        {
            get { return Config.UploadLimit; }
            set { base.TargetBandwidth = value; }
        }
    }

    class ThrottleDownloadStream : ThrottleStream
    {
        public ThrottleDownloadStream(Stream s) : base(s, double.NaN)
        {
        }

        protected override double TargetBandwidth
        {
            get { return Config.DownloadLimit; }
            set { base.TargetBandwidth = value; }
        }
    }

    class ThrottleStream : Stream, IHashStream
    {
        Stream innerStream;
        double _TargetBandwidth = double.PositiveInfinity;
        Dictionary<DateTime, int> TransRead = new Dictionary<DateTime, int>();
        Dictionary<DateTime, int> TransWrite = new Dictionary<DateTime, int>();
        long ReadTotal = 0;
        long WriteTotal = 0;
        const double ThrottleTimeSpan = 10;

        public ThrottleStream(Stream s, double bandwidth = double.PositiveInfinity) : base()
        {
            innerStream = s;
            TargetBandwidth = bandwidth;
        }

        protected virtual double TargetBandwidth
        {
            get { return (double.IsNaN(_TargetBandwidth) || _TargetBandwidth <= 0)? double.PositiveInfinity: _TargetBandwidth; }
            set { _TargetBandwidth = value; }
        }

        private void DoThrottleRead()
        {
            try
            {
                if (double.IsPositiveInfinity(TargetBandwidth)) return;
                foreach (var item in TransRead.Keys.Where(x => (DateTime.Now - x) > TimeSpan.FromSeconds(ThrottleTimeSpan)))
                {
                    ReadTotal -= TransRead[item];
                    TransRead.Remove(item);
                }
                var lasttime = TransRead.Keys.Min();
                double bandwidth = ReadTotal / (DateTime.Now - lasttime).TotalSeconds;
                if (TargetBandwidth < bandwidth)
                {
                    double waitsec = (ReadTotal / TargetBandwidth) - (DateTime.Now - lasttime).TotalSeconds;
                    waitsec = (waitsec > ThrottleTimeSpan) ? ThrottleTimeSpan : waitsec;
                    System.Threading.Thread.Sleep((int)(waitsec*1000));
                }
            }
            catch { }
        }

        private void DoThrottleWrite()
        {
            try
            {
                if (double.IsPositiveInfinity(TargetBandwidth)) return;
                foreach (var item in TransWrite.Keys.Where(x => (DateTime.Now - x) > TimeSpan.FromSeconds(ThrottleTimeSpan)))
                {
                    WriteTotal -= TransWrite[item];
                    TransWrite.Remove(item);
                }
                var lasttime = TransWrite.Keys.Min();
                double bandwidth = WriteTotal / (DateTime.Now - lasttime).TotalSeconds;
                if (TargetBandwidth < bandwidth)
                {
                    double waitsec = (ReadTotal / TargetBandwidth) - (DateTime.Now - lasttime).TotalSeconds;
                    waitsec = (waitsec > ThrottleTimeSpan) ? ThrottleTimeSpan : waitsec;
                    System.Threading.Thread.Sleep((int)(waitsec * 1000));
                }
            }
            catch { }
        }

        public override long Length { get { return innerStream.Length; } }
        public override bool CanRead { get { return innerStream.CanRead; } }
        public override bool CanWrite { get { return innerStream.CanWrite; } }
        public override bool CanSeek { get { return innerStream.CanSeek; } }
        public override void Flush() { innerStream.Flush(); }

        public override long Position
        {
            get
            {
                return innerStream.Position;
            }
            set
            {
                innerStream.Position = value;
            }
        }

        string IHashStream.Hash
        {
            get
            {
                return (innerStream as IHashStream).Hash;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            DoThrottleRead();
            int ret = innerStream.Read(buffer, offset, count);
            var time = DateTime.Now;
            ReadTotal += ret;
            if (TransRead.ContainsKey(time))
                TransRead[time] += ret;
            else
                TransRead.Add(time, ret);
            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            DoThrottleWrite();
            innerStream.Write(buffer, offset, count);
            var time = DateTime.Now;
            WriteTotal += count;
            if (TransWrite.ContainsKey(time))
                TransWrite[time] += count;
            else
                TransWrite.Add(time, count);
            TransWrite.Add(DateTime.Now, count);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }
    }
}
