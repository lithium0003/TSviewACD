using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Trace.Listeners.Add(new TextWriterTraceListener(System.IO.Path.Combine(Config.Config_BasePath, System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath)+".err.log")));
            Trace.AutoFlush = true;

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Trace.WriteLine(e.Exception);
            };

            if (args.Length == 0)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(Config.Language))
                    {
                        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(Config.Language);
                        Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
                    }
                }
                catch
                {
                    Config.Language = "";
                }
                MainForm = new Form1();
                Application.Run(MainForm);
                return 0;
            }
            else
            {
                var ret = ConsoleFunc.MainFunc(args);
                Console.Error.WriteLine(ret.ToString());
                return ret;
            }
        }
    }
}
