using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    static class Program
    {
        public static Form1 MainForm = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(System.IO.Path.ChangeExtension(Application.ExecutablePath, ".err.log")));
            Trace.AutoFlush = true;

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Trace.WriteLine(e.Exception);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0)
            {
                MainForm = new Form1();
                Application.Run(MainForm);
                return 0;
            }
            else
            {
                var ret = ConsoleFunc.MainFunc(args).Result;
                Console.Error.WriteLine(ret.ToString());
                return ret;
            }
        }
    }
}
