using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormLogWindow : Form
    {
        const int WM_GETTEXTLENGTH = 0x000E;
        const int EM_SETSEL = 0x00B1;
        const int EM_REPLACESEL = 0x00C2;

        [DllImport("User32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint uMsg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private readonly SynchronizationContext synchronizationContext;

        private TextWriter LogStream;
        public bool LogToFile
        {
            get { return LogStream != null; }
            set
            {
                if (value)
                {
                    if (LogStream == null)
                    {
                        try
                        {
                            LogStream = TextWriter.Synchronized(new StreamWriter(Stream.Synchronized(new FileStream(Path.Combine(Config.Config_BasePath, Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".log"), FileMode.Append, FileAccess.Write, FileShare.Read))));
                        }
                        catch { }
                    }
                }
                else
                {
                    if (LogStream != null)
                    {
                        LogStream.Flush();
                        LogStream = null;
                    }
                }
            }
        }

        public FormLogWindow()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;
        }

        public void LogOut(string str)
        {
            lock (this)
            {
                if (Config.debug)
                    Console.Error.WriteLine(str);
                str = string.Format("[{0}] {1}\r\n", DateTime.Now.ToString(), str);
                LogStream?.Write(str);
                LogStream?.Flush();
                if (Config.IsClosing) return;
                synchronizationContext.Post(
                    (o) =>
                    {
                        if (Config.IsClosing) return;
                        SendMessage(textBox1.Handle, EM_REPLACESEL, 1, o as string);
                    }, str);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void FormLogWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            if (e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (Owner != null && StartPosition == FormStartPosition.CenterParent)
            {
                int offset = Owner.OwnedForms.Length * 38;  // approx. 10mm
                Point p = new Point(Owner.Left + Owner.Width / 2 - Width / 2 + offset, Owner.Top + Owner.Height / 2 - Height / 2 + offset);
                Location = p;
            }
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            textBox1.Select(textBox1.Text.Length, 0);
        }
    }

    class LogWindowStream : Stream
    {
        FormLogWindow log;
        string strbuf = "";

        public LogWindowStream(FormLogWindow window)
        {
            log = window;
        }
        public override long Length { get { return -1; } }
        public override bool CanRead { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override long Position {
            get { return -1; }
            set { }
        }
        public override void SetLength(long value) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return -1;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return -1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            string buf1 = Encoding.UTF8.GetString(buffer, offset, count);
            if (buf1.Contains("\r\n"))
            {
                foreach(var line in buf1.Split(new char[] { '\r', '\n' }))
                {
                    if (line == "")
                    {
                        LogOutput();
                    }
                    else
                    {
                        strbuf += line;
                    }
                }
            }
            else
            {
                strbuf += buf1;
            }
        }

        public override void Flush() { LogOutput(); }

        private void LogOutput()
        {
            if (strbuf == "") return;
            log.LogOut("[FFmpeg]"+strbuf);
            strbuf = "";
        }
    }
}
