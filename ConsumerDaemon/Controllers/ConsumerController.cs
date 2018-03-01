using RabbitMqTaskDemo;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ConsumerDaemon.Controllers
{
    [Produces("application/json")]
    [Route("api/Consumer")]
    public class ConsumerController : Controller
    {
        public static LongRunningTaskController Controller;
        private const string ConsumersMissingMessage = "ERROR: Must add consumers first.";

        [HttpGet]
        public ResultObject Get()
        {
            if (Controller == null) return createMissingConsumersMessage();
            var result = JsonConvert.SerializeObject(new List<ILongRunningTask>(Controller.LongRunningTasks), new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            return createResultObject("Listing of Consumer objects.", result);
        }

        [HttpGet("add/{numberOfConsumersToAdd}/{queueName}")]
        public ResultObject Add(int numberOfConsumersToAdd, string queueName)
        {

            Controller = new LongRunningTaskController();

            var connection = new RabbitMqConnection();
            connection.Host = "192.168.0.101";

            var queue = new RabbitMqQueue();
            queue.Name = queueName;
            queue.AutoAck = false;

            for (int i = 0; i < numberOfConsumersToAdd; i++)
            {
                var consumer = new BasicConsumerTask();
                //consumer.Connection = connection;
                consumer.Queue = queue;
                Controller.AddLongRunningTask(consumer);
            }
            
            return createResultObject(Controller.LongRunningTasks.Count() + " Consumers Created", Controller.LongRunningTasks);
        }

        [HttpGet("restart")]
        public ResultObject Restart()
        {
            if (Controller == null) return createMissingConsumersMessage();
            Controller.StartAll();
            Thread.Sleep(5000);
            int runningConsumerCount = 0;
            foreach (var consumer in Controller.LongRunningTasks)
            {
                if (consumer.IsRunning) runningConsumerCount++;
            }

            return  createResultObject( runningConsumerCount + " Consumers running!", Controller.LongRunningTasks);
        }

        [HttpGet("start")]
        public ResultObject Start()
        {
            if (Controller == null) return createMissingConsumersMessage();
            Controller.StartAll();
            Thread.Sleep(5000);
            int runningConsumerCount = 0;
            foreach (var consumer in Controller.LongRunningTasks)
            {
                if (consumer.IsRunning) runningConsumerCount++;
            }

            return createResultObject(runningConsumerCount + " Consumers running!", Controller.LongRunningTasks);
        }

        [HttpGet("stop")]
        public ResultObject Stop()
        {
            if (Controller == null) return createMissingConsumersMessage();
            Controller.StopAll();
            
            int runningConsumerCount = 0;
            foreach (var consumer in Controller.LongRunningTasks)
            {
                if (!consumer.IsRunning) runningConsumerCount++;
            }

            return createResultObject(runningConsumerCount + " Consumers are stopped!", Controller.LongRunningTasks);
        }

        [HttpGet("running")]
        public ResultObject Running()
        {
            if (Controller == null) return createMissingConsumersMessage();
           
            int runningConsumerCount = 0;
            foreach (var consumer in Controller.LongRunningTasks)
            {
                if (consumer.IsRunning) runningConsumerCount++;
            }

            return createResultObject(runningConsumerCount + " Consumers are running.", Controller.LongRunningTasks);
        }

        [HttpGet("logs")]
        public ResultObject Logs()
        {
            if (Controller == null)
            {
                return createMissingConsumersMessage();
            }
            return createResultObject("Top 10 log entries, newest to oldest.", 
                Controller.Logger.InMemoryLogs.OrderByDescending(e => e.TimeStamp).Take(10).ToList());
        }

        [HttpGet("exceptions")]
        public ResultObject Exceptions()
        {
            if (Controller == null)
            {
                return createMissingConsumersMessage();
            }
            
            return createResultObject("Top 10 exceptions, newest to oldest." , Controller.Logger.InMemoryLogs.Where(x => x.IsException == true).OrderByDescending(e => e.TimeStamp).Take(10).ToList());
        }

        private ResultObject createMissingConsumersMessage()
        {
            var log = new ResultObject();
            log.Details = ConsumersMissingMessage;
            return log;
        }

        private ResultObject createResultObject(string message, object payload)
        {
            var o = new ResultObject();
            o.Details = message;
            o.Payload = payload;
            return o;
        }

    }

    public class ResultObject
    {
        public string Details { get; set; }
        public object Payload { get; set; }

    }

}
