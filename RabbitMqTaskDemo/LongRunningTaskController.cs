using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace RabbitMqTaskDemo
{
    public class LongRunningTaskController : ILongRunningTaskController
    {
        /// <summary>
        /// ConcurrentBag facilitates safe access when this class is maintained as a singleton inside a hosted container (web app)
        /// </summary>
        public ConcurrentBag<ILongRunningTask> LongRunningTasks { get; set; } = new ConcurrentBag<ILongRunningTask>();
        /// <summary>
        /// NLog is thread safe....
        /// </summary>
        public LoggerWrapper Logger { get; set; } = new LoggerWrapper("RabbitMqConsumer");  
        
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
      
        public void AddLongRunningTask(ILongRunningTask task)
        {
            task.Logger = Logger;
            LongRunningTasks.Add(task);           
        }

        public void StartAll()
        {
            try
            {
                foreach (var consumer in LongRunningTasks)
                {
                    consumer.Start(_tokenSource.Token);
                }
            }
            catch (Exception e)
            {
                var logEntry = new LongRunningTaskLogEntry();
                logEntry.Details.Add("StackTrace",e.StackTrace);
                Logger.Log(logEntry);
            }
        }

        public void StopAll()
        {
           
            _tokenSource.Cancel();
            LongRunningTasks = new ConcurrentBag<ILongRunningTask>();
            _tokenSource.Dispose();
            _tokenSource = new CancellationTokenSource();
        }

       
      

    }
}
