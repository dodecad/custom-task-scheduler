using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomTaskScheduler
{
    /// <summary>
    /// A class that implements the logic to schedule the running of the underlying threads.
    /// </summary>
    /// <seealso cref="System.Threading.Tasks.TaskScheduler" />
    public class CustomTaskScheduler : TaskScheduler
    {
        [ThreadStatic]
        private static bool _itemsProcessingInCurrentThread;

        private readonly int _maxDegreeOfParallelism;
        private readonly LinkedList<Task> _tasks;

        private volatile int _runningOrQueuedCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTaskScheduler"/> class.
        /// </summary>
        public CustomTaskScheduler()
            : this(Environment.ProcessorCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTaskScheduler"/> class.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
        public CustomTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            this._maxDegreeOfParallelism = maxDegreeOfParallelism;
            this._tasks = new LinkedList<Task>();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            var lockTaken = false;

            try
            {
                Monitor.TryEnter(this._tasks, ref lockTaken);

                if (lockTaken)
                    return this._tasks.ToArray();
                else
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(this._tasks);
            }
        }

        protected override void QueueTask(Task task)
        {
            lock (this._tasks)
            {
                this._tasks.AddLast(task);
            }

            if (this._runningOrQueuedCount >= this._maxDegreeOfParallelism)
                return;

            this._runningOrQueuedCount++;
            this.RunTasks();
        }

        protected override bool TryDequeue(Task task)
        {
            lock (this._tasks)
            {
                return this._tasks.Remove(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool previouslyQueued)
        {
            if (CustomTaskScheduler._itemsProcessingInCurrentThread == false)
                return false;

            if (previouslyQueued)
                this.TryDequeue(task);

            return base.TryExecuteTask(task);
        }

        private void RunTasks()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                var taskList = new List<Task>();

                CustomTaskScheduler._itemsProcessingInCurrentThread = true;

                try
                {
                    while (true)
                    {
                        lock (this._tasks)
                        {
                            if (this._tasks.Count == 0)
                            {
                                this._runningOrQueuedCount--;
                                break;
                            }

                            var task = this._tasks.First.Value;
                            taskList.Add(task);
                            this._tasks.RemoveFirst();
                        }
                    }

                    if (taskList.Count == 1)
                    {
                        base.TryExecuteTask(taskList[0]);
                    }
                    else if (taskList.Count > 0)
                    {
                        var batches = taskList.GroupBy(task => taskList.IndexOf(task) / this._maxDegreeOfParallelism);

                        foreach (var batch in batches)
                            batch.AsParallel().ForAll(task => base.TryExecuteTask(task));
                    }
                }
                finally
                {
                    CustomTaskScheduler._itemsProcessingInCurrentThread = false;
                }
            }, null);
        }
    }
}
