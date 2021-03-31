using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace PWM
{
    public class UnityTaskScheduler : TaskScheduler
    {
        public static new TaskScheduler Default { get; } = new UnityTaskScheduler();
        public static TaskFactory Factory { get; } = new TaskFactory(Default);

        private ConcurrentQueue<Task> tasks = new ConcurrentQueue<Task>();

        public bool IsRunning { get; private set; } = false;
        public bool Cancelling { get; private set; } = false;

        public static Thread mainThread { get; set; }


        public void Start()
        {
            foreach (var task in tasks)
            {
                TryExecuteTask(task);
            }
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            tasks.Enqueue(task);
        }


        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread != mainThread)
            {
                MelonLoader.MelonLogger.Msg("Not running on main thread");
                return false;
            }
            return TryExecuteTask(task);
        }
    }
}
