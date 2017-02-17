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

        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);

        private readonly SynchronizationContext synchronizationContext;

        private StreamWriter LogStream;
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
                            LogStream = new StreamWriter(new FileStream(Path.ChangeExtension(Application.ExecutablePath, "log"), FileMode.Append, FileAccess.Write, FileShare.Read));
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
            if(Config.debug)
                Console.Error.WriteLine(str);
            str = string.Format("[{0}] {1}\r\n", DateTime.Now.ToString(), str);
            LogStream?.Write(str);
            LogStream?.Flush();
            synchronizationContext.Post(
                (o) =>
                {
                    if (Config.IsClosing) return;
                    SendMessage(textBox1.Handle, EM_REPLACESEL, 1, o as string);
                }, str);
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
}
