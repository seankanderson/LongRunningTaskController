using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqTaskDemo
{
    public class BasicConsumerTask : ILongRunningTask
    {

        public LoggerWrapper Logger { get; set; }

        public RabbitMqConnection Connection { private get; set; }
        public RabbitMqQueue Queue { private get; set; }
        public RabbitMqExchange Exchange { private get; set; }
        private string _consumerId;
        private IConnection _connection;
        private IModel _channel;
        private CancellationToken _cancellationToken;
        EventingBasicConsumer _consumer;

        public bool IsRunning { get; private set; }

        private int _messageCount;
        public int ExecutionCount { get { return _messageCount; } set { lock (this) { _messageCount++; } } }

        public Task Start(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            if (IsRunning)
            {
                throw new Exception("Consumer is already running.  Start failed.");
            }

            var task = new Task(() => StartAction(), _cancellationToken, TaskCreationOptions.LongRunning);
            task.ConfigureAwait(false);
            task.Start();
            return task;
        }

        private void StartAction()
        {
            try
            {
                CancellationTokenRegistration tokenRegistration =
                       _cancellationToken.Register(() => Stop());
                X509Store store = null;
                try
                {
                    store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly);
                }
                catch (Exception e)
                {
                    LogException("Problem opening Windows Cert Store.", e.StackTrace);
                }

                var Tls = new SslOption()
                {
                    CertPath = Connection.CertPath,
                    Certs = Connection.CertPath.Equals("WindowsCertStore") ? store.Certificates : null,
                    CertPassphrase = Connection.CertPassphrase,
                    ServerName = Connection.CertServerName,
                    Enabled = Connection.TlsEnabled
                };

                var factory = new ConnectionFactory()
                {
                    UserName = Connection.User,
                    Password = Connection.Password,
                    VirtualHost = Connection.VHost,
                    HostName = Connection.HostName,
                    Ssl = Tls,
                    Port = Connection.Port
                };

                _connection = factory.CreateConnection();

                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                       queue: Queue.Name,
                       durable: Queue.Durable,
                       exclusive: Queue.Exclusive,
                       autoDelete: Queue.AutoDelete,
                       arguments: Queue.Arguments);
                
                if (Exchange.Name != RabbitMqExchange.DefaultExchange && !Exchange.Name.StartsWith("amq."))
                {
                    _channel.ExchangeDeclare(
                        exchange: Exchange.Name,
                        type: Exchange.Type,
                        durable: Exchange.Durable,
                        autoDelete: Exchange.AutoDelete,
                        arguments: Exchange.Arguments);

                    _channel.QueueBind(
                            queue: Queue.Name,
                            exchange: Exchange.Name,
                            routingKey: Queue.RoutingKey,
                            arguments: null);
                }

                _consumer = new EventingBasicConsumer(_channel);
                _consumerId = _consumer.ConsumerTag = Guid.NewGuid().ToString();
                _consumer.Received += ConsumerDelegate;

                _channel.BasicQos(0, Connection.Prefetch, true);
                _consumerId = _channel.BasicConsume(queue: Queue.Name, autoAck: Queue.AutoAck, consumer: _consumer);  //start the consumer
                IsRunning = true;
            }
            catch (Exception e)
            {
                LogException("Problem creating connection/channel/queue.", e.StackTrace);
                Stop();
            }
        }


        private void ConsumerDelegate(object model, BasicDeliverEventArgs eventArgs)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var messageBody = Encoding.UTF8.GetString(eventArgs.Body);

            try
            {
                if (_channel.IsClosed || IsRunning == false)
                {
                    _connection.Close();
                    return;
                }

                //Do work
                var bodyObject = JsonConvert.SerializeObject(messageBody, Formatting.Indented);
                 
                Console.WriteLine(bodyObject);
                if (Queue.AutoAck == false)
                {
                    _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                    ExecutionCount = 0;
                }
                LogMessageSuccess(stopwatch.ElapsedMilliseconds, messageBody);

            }
            catch (Exception e)
            {

                LogException("Problem acknowledging message.", e.StackTrace);
                Stop();

                //channel.BasicReject(ea.DeliveryTag, false);  //server discards the message
                //channel.BasicReject(ea.DeliveryTag, true);  //server will resend the message to any subscriber
            }

        }

        public void Stop()

        {
            var logEntry = CreateLogEntry();
            logEntry.Details.Add("Comment", "Stopping Consumer");
            Logger.Log(logEntry);

            try
            {
                if (_channel.IsOpen)
                {
                    _channel.BasicCancel(_consumerId);
                    _channel.Abort();
                    _connection.AutoClose = true;
                }
                _consumer.HandleBasicCancelOk(_consumerId);
                IsRunning = false;
            }
            catch (Exception e)
            {
                LogException("Problem closing channel on kill", e.StackTrace);
            }
        }

        private void LogMessageSuccess(long elapsedTimeInMs, string messageBody)
        {
            var logEntry = CreateLogEntry();
            logEntry.ProcessingTimeInMs = elapsedTimeInMs;
            logEntry.Details.Add("Comment", "Success");
            logEntry.Details.Add("MessageBody", messageBody);
            Logger.Log(logEntry);
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
            //logEntry.Details.Add("RabbitMqNode", Connection.Host + ":" + Connection.Port);
            logEntry.Details.Add("ConsumerId", _consumerId);
            logEntry.Details.Add("Queue", Queue.Name);
            return logEntry;
        }



    }
}
