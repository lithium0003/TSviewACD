using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSviewACD
{
    class TaskCanselToken
    {
        public CancellationTokenSource cts = new CancellationTokenSource();
        public string taskname;

        public TaskCanselToken() { }
        public TaskCanselToken(string name)
        {
            taskname = name;
        }
    }
}
