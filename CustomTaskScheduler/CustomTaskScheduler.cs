using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CustomTaskScheduler
{
    public class CustomTaskScheduler : TaskScheduler
    {
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected override void QueueTask(Task task)
        {
            throw new NotImplementedException();
        }

        protected override bool TryExecuteTaskInline(Task task, bool previouslyQueued)
        {
            throw new NotImplementedException();
        }
    }
}
