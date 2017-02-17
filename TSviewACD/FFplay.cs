using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    public class ffplayEOF_CanceledException : OperationCanceledException
    {
    }

    public class FFplay_process
    {
        const string exename = "ffplay.exe";
        Process coProcess;
        static string commandline;

        public FFplay_process(string options = null)
        {
            if (!string.IsNullOrEmpty(options)) commandline = options;
            commandline = fixArguments(commandline);

            var p = new Process();
            p.StartInfo.FileName = exename;
            p.StartInfo.Arguments = commandline;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            if (p.Start()) coProcess = p;
        }

        private string fixArguments(string org_str)
        {
            // remove newline for combile lines
            var str = org_str.Trim().Replace("\r", "").Replace("\n", "");
            // remove exe name
            if (str.StartsWith(exename)) str = str.Substring(exename.Length);
            if (str.StartsWith(Path.GetFileNameWithoutExtension(exename))) str = str.Substring(Path.GetFileNameWithoutExtension(exename).Length);

            str = " " + str + " ";
            // input file must be pipe
            if (!str.Contains(" - ") && !str.Contains(" -i pipe:0 ") && !str.Contains(" -i pipe ")) str = " - " + str;
            // add autoexit
            if (!str.Contains(" -autoexit ")) str += " -autoexit ";
            return str.Trim();
        }

        public void Write(byte[] buffer, int index, int count)
        {
            if (coProcess?.HasExited == false)
            {
                try
                {
                    coProcess.StandardInput.BaseStream.Write(buffer, index, count);
                }
                catch (IOException)
                {
                    throw new ffplayEOF_CanceledException();
                }
            }
            else
            {
                throw new ffplayEOF_CanceledException();
            }
        }

        public async Task Finish(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (coProcess == null) return;
            if (coProcess.HasExited) return;
            coProcess.StandardInput.BaseStream.Flush();
            coProcess.StandardInput.Close();
            while (!coProcess.HasExited)
                await Task.Delay(1000, cancellationToken);
        }

        public void Kill()
        {
            if (coProcess == null) return;
            if (coProcess.HasExited) return;
            coProcess.CloseMainWindow();
            if (coProcess.HasExited) return;
            coProcess.Kill();
            while (!coProcess.HasExited) Thread.Sleep(1000);
        }

        public void AddExitFunction(EventHandler func)
        {
            if (coProcess == null) return;
            coProcess.Exited += func;
        }
    }

    public class WriteToFFplayEventArgs : EventArgs
    {
        public long Position;
    }

    public delegate void WriteToFFplayEventHandler(object sender, WriteToFFplayEventArgs e);


    public class FFplay_Stream : Stream
    {
        long _Position;
        FFplay_process ffplay;

        public FFplay_Stream(FFplay_process FFplay = null)
        {
            _Position = 0;
            ffplay = FFplay ?? new FFplay_process();
        }

        public override long Length { get { throw new NotSupportedException("not supported Length"); } }
        public override bool CanRead { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override void Flush() { }

        public override long Position
        {
            get
            {
                return _Position;
            }
            set
            {
                throw new NotSupportedException("not supported SetPosition");
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("not supported Read");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("not supported seek");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("not supported SetLength");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ffplay.Write(buffer, offset, count);
            _Position += (count - offset);
            data.Position = _Position;
            WriteToFFplayEvent?.Invoke(this, data);
        }

        public event WriteToFFplayEventHandler WriteToFFplayEvent;
        private WriteToFFplayEventArgs data = new WriteToFFplayEventArgs();

    }
}
