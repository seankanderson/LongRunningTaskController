using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqQueue
    {

        public string Name { get; set; } = String.Empty;
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public bool AutoAck { get; set; }
        private string _routingKey = String.Empty;
        public string RoutingKey
        {
            get
            {
                return (_routingKey == String.Empty) ? Name : _routingKey;
            }
            set
            {
                _routingKey = value;
            }
        }

        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}
