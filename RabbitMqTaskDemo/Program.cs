using System;

namespace RabbitMqTaskDemo
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //change to 1 task when debugging breakpoints
            int numberOfTasksToAdd = 10;
            string taskType = String.Empty;

            if (args.Length > 0)
            {
                taskType = args[0];
                Int32.TryParse(args[1], out numberOfTasksToAdd);
            }

            ILongRunningTaskController Controller = new LongRunningTaskController();

            var connection = new RabbitMqConnection();
            connection.Host = "192.168.0.101";
                        
            var queue = new RabbitMqQueue();
            queue.Name = "TestQueue01";
            queue.AutoAck = false;

            var queue2 = new RabbitMqQueue();
            queue2.Name = "TestQueue02";
            queue2.AutoAck = false;

            var defaultExchange = new RabbitMqExchange();

            var fanoutExchange = new RabbitMqExchange();
            fanoutExchange.Type = "fanout";
            fanoutExchange.Name = "amq.fanout";

            queue.BindingExchange = fanoutExchange;
            queue2.BindingExchange = fanoutExchange;

            char ch = ' ';
            if (taskType == String.Empty)
            {
                Console.WriteLine("Press P to publish or C to consume.");
                 ch = Console.ReadKey().KeyChar;
            }

            if (ch == 'c' || ch == 'C' || taskType == "consume")
            {                
                Console.WriteLine("\nStarting {0} Consumers", numberOfTasksToAdd);
                for (int i = 0; i < numberOfTasksToAdd; i++)
                {
                    var consumer = new BasicConsumerTask();
                    consumer.Connection = connection;
                    consumer.Queue = queue;
                    Controller.AddLongRunningTask(consumer);
                }

            }
            else if (ch == 'p' || ch == 'P' || taskType == "publish")
            {
                Console.WriteLine("\nStarting {0} Publishers", numberOfTasksToAdd);
                for (int i = 0; i < numberOfTasksToAdd; i++)
                {
                    var publisher = new BasicPublisherTask();
                    publisher.Connection = connection;
                    publisher.Queues.Add(queue);
                    publisher.Queues.Add(queue2);
                    publisher.ConfirmPublishedMessages = true;
                    Controller.AddLongRunningTask(publisher);
                }
            }
            
            Console.WriteLine("Press K to KILL all tasks.");

            Controller.StartAll();

            ch = Console.ReadKey().KeyChar;
            if (ch == 'k' || ch == 'K')
            {
                Controller.StopAll();
                Console.WriteLine("\nTask cancellation requested.");

            }

            Console.WriteLine("\nMAIN THREAD FINISHED");
        }


    }
}
