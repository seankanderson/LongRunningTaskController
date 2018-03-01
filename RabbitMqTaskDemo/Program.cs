using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.IO;

namespace RabbitMqTaskDemo
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">First argument is the configuration file and the second argument is a payload file for the message body to publish.</param>
        static void Main(string[] args)
        {
            var parameterFile = "parameters.json";
            var payloadFile = "";
            var payload = String.Empty;

            RabbitMqTaskDemoParameters parameters =
                new RabbitMqTaskDemoParameters()
                {
                    Connection = new RabbitMqConnection(),
                    Queue = new RabbitMqQueue(),
                    Exchange = new RabbitMqExchange(),
                    NumberOfTasks = 1,                    
                    Role = "consumer"
                };

            if (args == null || args.Length == 0)
            {
                try
                {
                    parameters = JsonConvert.DeserializeObject<RabbitMqTaskDemoParameters>(File.ReadAllText(parameterFile));
                    payload = File.ReadAllText(payloadFile);
                }
                catch (Exception)
                {
                    File.WriteAllText(parameterFile, JsonConvert.SerializeObject(parameters, Formatting.Indented));
                }
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    parameters = JsonConvert.DeserializeObject<RabbitMqTaskDemoParameters>(File.ReadAllText(args[0]));
                    Console.WriteLine("Config file: {0}", args[0]);
                    payload = File.ReadAllText(args[1]);
                    Console.WriteLine("Payload file: {0}", args[1]);
                }
                catch (Exception)
                {
                }
            }

            payload = payload == String.Empty ? JsonConvert.SerializeObject(parameters, Formatting.Indented) : payload;

            Console.WriteLine("config: {0}", JsonConvert.SerializeObject(parameters, Formatting.Indented));
            Console.WriteLine("payload: {0}", JsonConvert.SerializeObject(payload, Formatting.Indented));

            //TODO: Add logic to suck in a text file for use as the message body
            //TODO: Create channel configuration for publisher confirms

            ILongRunningTaskController Controller = new LongRunningTaskController();

            if (parameters.Role == "consumer")
            {
                Console.WriteLine("\nStarting {0} Consumers", parameters.NumberOfTasks);
                for (int i = 0; i < parameters.NumberOfTasks; i++)
                {
                    var consumer = new BasicConsumerTask();
                    consumer.Connection = parameters.Connection;
                    consumer.Queue = parameters.Queue;
                    consumer.Exchange = parameters.Exchange;
                    Controller.AddLongRunningTask(consumer);
                }

            }
            else if (parameters.Role == "publisher")
            {
                Console.WriteLine("\nStarting {0} Publishers", parameters.NumberOfTasks);
                for (int i = 0; i < parameters.NumberOfTasks; i++)
                {
                    var publisher = new BasicPublisherTask();
                    publisher.Connection = parameters.Connection;
                    publisher.Queue = parameters.Queue;
                    publisher.Exchange = parameters.Exchange;
                    publisher.Payload = payload;
                    Controller.AddLongRunningTask(publisher);
                }
            }

            Console.WriteLine("Press K to KILL all tasks.");

            Controller.StartAll();

            char ch = Console.ReadKey().KeyChar;
            if (ch == 'k' || ch == 'K')
            {
                Controller.StopAll();
                Console.WriteLine("\nTask cancellation requested.");

            }

            Console.WriteLine("\nMAIN THREAD FINISHED");
        }


    }
}
