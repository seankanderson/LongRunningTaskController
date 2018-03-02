using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqTaskDemo
{
    public class BasicPublisherTask : ILongRunningTask
    {
        public LoggerWrapper Logger { get; set; }
        public bool IsRunning { get; private set; }
        public RabbitMqConnection Connection { private get; set; }
        public RabbitMqQueue Queue { get; set; }
        public RabbitMqExchange Exchange { get; set; }
        public int ExecutionCount { get { return _messageCount; } set { lock (this) { _messageCount = _messageCount + value; } } }
        public string Payload { get; set; } = String.Empty;
        private CancellationToken _cancellationToken;
        private IConnection _connection;
        private IModel _channel;
        private IBasicProperties _channelProperties;
        private int _messageCount = 0;
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

            var factory = new ConnectionFactory() {
                UserName = Connection.User,
                Password = Connection.Password,
                VirtualHost =Connection.VHost,
                HostName = Connection.HostName,
                Ssl = Tls,
                Port = Connection.Port
            };
            
            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();
                        
            _channelProperties = _channel.CreateBasicProperties();
            _channelProperties.DeliveryMode = Exchange.MessageDeliveryMode;

            if (Connection.PublisherConfirmation) _channel.ConfirmSelect();

            if (Queue.Name != String.Empty)
            {
                _channel.QueueDeclare(
                     queue: Queue.Name,
                     durable: Queue.Durable,
                     exclusive: Queue.Exclusive,
                     autoDelete: Queue.AutoDelete,
                     arguments: Queue.Arguments);
            }
            if (Exchange.Name != RabbitMqExchange.DefaultExchange && !Exchange.Name.StartsWith("amq."))
            {
                _channel.ExchangeDeclare(
                    exchange: Exchange.Name,
                    type: Exchange.Type,
                    durable: Exchange.Durable,
                    autoDelete: Exchange.AutoDelete,
                    arguments: Exchange.Arguments);

                _channel.BasicReturn += _channel_BasicReturn;

                if (Queue.Name != String.Empty)
                {
                    _channel.QueueBind(
                            queue: Queue.Name,
                            exchange: Exchange.Name,
                            routingKey: Queue.RoutingKey,
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

        private void _channel_BasicReturn(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs e)
        {
            ExecutionCount = -1;
            Console.WriteLine("MESSAGE NOT ROUTABLE: No Queue bound to exchange." );
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
                var messageBody = Guid.NewGuid().ToString();
                if (Payload != String.Empty)
                {
                    messageBody = Payload;
                }

                var json = JsonConvert.SerializeObject(messageBody);
                var body = Encoding.UTF8.GetBytes(json);
                                
                _channel.BasicPublish(exchange: Exchange.Name,
                                     routingKey: Queue.RoutingKey,
                                     basicProperties: _channelProperties,
                                     mandatory: true,
                                     body: body);
                if (Connection.PublisherConfirmation) _channel.WaitForConfirms();
                watch.Stop();
                ExecutionCount = 1;
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
            logEntry.Details.Add("RabbitMqNode", Connection.HostName + ":" + Connection.Port);

            return logEntry;
        }


    }
}
