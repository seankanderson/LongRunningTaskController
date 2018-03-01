using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqExchange
    {
        public static string DefaultExchange = String.Empty;

        public string Name { get; set; } = DefaultExchange;
        public string Type { get; set; } // Direct / Topic / Fanout / Header
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public bool Internal { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
        
    }
}
