using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqTaskDemo
{
    public class BasicPublisherTask : ILongRunningTask
    {
        public LoggerWrapper Logger { get; set; }
        public bool IsRunning { get; private set; }
        public bool ConfirmPublishedMessages { get; set; }
        public RabbitMqConnection Connection { private get; set; }
        public List<RabbitMqQueue> Queues { get; set; } = new List<RabbitMqQueue>();
        private CancellationToken _cancellationToken;
        private IConnection _connection;
        private IModel _channel;

        public Task Start(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            if (IsRunning)
            {
                throw new Exception("Publisher is already running.  Start failed.");
            }

            var task = new Task(() => StartAction()
            , _cancellationToken
            , TaskCreationOptions.LongRunning);
            task.ConfigureAwait(false);
            task.Start();
            return task;
        }

        private void StartAction()
        {
            CancellationTokenRegistration tokenRegistration =
                        _cancellationToken.Register(() => Stop());

            var factory = new ConnectionFactory() { HostName = Connection.Host, Port = Connection.Port };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            if (ConfirmPublishedMessages) _channel.ConfirmSelect();

            foreach (var queue in Queues)
            {
                _channel.QueueDeclare(
                        queue: queue.Name,
                        durable: queue.Durable,
                        exclusive: queue.Exclusive,
                        autoDelete: queue.AutoDelete,
                        arguments: queue.Arguments);

                if (queue.BindingExchange.Name != RabbitMqExchange.DefaultExchange)
                {
                    if (!queue.BindingExchange.Name.StartsWith("amq."))
                    {
                        _channel.ExchangeDeclare(
                            exchange: queue.BindingExchange.Name,
                            type: queue.BindingExchange.Type,
                            durable: queue.BindingExchange.Durable,
                            autoDelete: queue.BindingExchange.AutoDelete,
                            arguments: queue.BindingExchange.Arguments);
                    }
                    _channel.QueueBind(
                        queue: queue.Name,
                        exchange: queue.BindingExchange.Name,
                        routingKey: queue.RoutingKey,
                        arguments: null);
                }
            }
            IsRunning = true;
            while (1 == 1)
            {
                try
                {
                    Publish();
                }
                catch (AlreadyClosedException)
                {
                    //gracefully shutdown after stopping all threads
                }
            }
        }
        private void Stop()
        {
            var logEntry = CreateLogEntry();
            logEntry.Details.Add("Comment", "Stopping Consumer");
            Logger.Log(logEntry);

            try
            {
                if (_channel.IsOpen)
                {
                    _channel.Abort();
                    _connection.AutoClose = true;
                }

                IsRunning = false;
            }
            catch (Exception e)
            {
                LogException("Problem closing channel on kill", e.StackTrace);
            }
        }

        private void Publish()
        {
            try
            {
                if (_channel.IsClosed || IsRunning == false)
                {
                    if (_connection.IsOpen) _connection.Close();
                    return;
                }

                var watch = new Stopwatch();

                foreach (var queue in Queues)
                {
                    var messageBody = Guid.NewGuid().ToString();
                    var json = JsonConvert.SerializeObject(queue.Name +": "+ messageBody);
                    var body = Encoding.UTF8.GetBytes(json);

                    _channel.BasicPublish(exchange: queue.BindingExchange.Name,
                                         routingKey: queue.RoutingKey,
                                         basicProperties: null,
                                         body: body);
                    if (ConfirmPublishedMessages) _channel.WaitForConfirms();
                    Debug.WriteLine("Ms: {0} PUBLISHED: " + json, watch.ElapsedMilliseconds);
                }
                watch.Stop();
                
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                throw e;
            }
        }

        private void LogException(string comment, string stackTrace)
        {
            var exceptionLog = CreateLogEntry();
            exceptionLog.IsException = true;
            exceptionLog.Details.Add("Comment", comment);
            exceptionLog.Details.Add("StackTrace", stackTrace);
            Logger.Log(exceptionLog);
        }

        private LongRunningTaskLogEntry CreateLogEntry()
        {
            var logEntry = new LongRunningTaskLogEntry();
            logEntry.Details.Add("RabbitMqNode", Connection.Host + ":" + Connection.Port);
                        
            return logEntry;
        }


    }
}
