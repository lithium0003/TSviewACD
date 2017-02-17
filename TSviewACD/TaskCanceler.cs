using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    public class TaskCanselToken
    {
        public CancellationTokenSource cts = new CancellationTokenSource();
        public string taskname;

        public TaskCanselToken() { }
        public TaskCanselToken(string name)
        {
            taskname = name;
        }
    }

    public class TaskCanceler
    {
        static public TaskCanselToken CreateTask(string taskname)
        {
            if (Program.MainForm == null)
                return ConsoleFunc.CreateTask(taskname);
            else
                return Program.MainForm.CreateTask(taskname);
        }

        static public void FinishTask(TaskCanselToken task)
        {
            if (Program.MainForm == null)
                ConsoleFunc.FinishTask(task);
            else
                Program.MainForm.FinishTask(task);
        }

        static public async Task CancelTask(string taskname)
        {
            if (Program.MainForm == null)
                await ConsoleFunc.CancelTask(taskname).ConfigureAwait(false);
            else
                await Program.MainForm.CancelTask(taskname);
        }

        static public bool CancelTaskAll()
        {
            if (Program.MainForm == null)
                return ConsoleFunc.CancelTaskAll();
            else
                return Program.MainForm.CancelTaskAll();
        }
    }
}
