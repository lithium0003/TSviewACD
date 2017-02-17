using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Trace.Listeners.Add(new TextWriterTraceListener(System.IO.Path.ChangeExtension(Application.ExecutablePath, ".err.log")));
            Trace.AutoFlush = true;

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Trace.WriteLine(e.Exception);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
