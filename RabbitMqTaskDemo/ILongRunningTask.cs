using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqTaskDemo
{
    public interface ILongRunningTask
    {
        LoggerWrapper Logger { get; set; }
        Task Start(CancellationToken cancellationToken);
        bool IsRunning { get; }
        int ExecutionCount { get; set; }
       
    }
}
