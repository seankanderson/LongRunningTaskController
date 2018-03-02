using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMqTaskDemo
{
    public interface ILongRunningTaskController
    {
        ConcurrentBag<ILongRunningTask> LongRunningTasks { get; set; }
        LoggerWrapper Logger { get; set; }
        void AddLongRunningTask(ILongRunningTask task);
        int GetExecutionCount();
        void StartAll();
        void StopAll();

    }
}
