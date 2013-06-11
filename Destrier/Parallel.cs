using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Destrier
{
    public class Parallel
    {
        public static void Execute(params Action[] toExecute)
        {
            Task[] tasks = new Task[toExecute.Length];

            Int32 index = 0;
            foreach (Action a in toExecute)
            {
                tasks[index] = new Task(a);
                index++;
            }
            foreach (Task t in tasks)
            {
                t.Start();
            }

            Task.WaitAll(tasks);
        }
    }
}
