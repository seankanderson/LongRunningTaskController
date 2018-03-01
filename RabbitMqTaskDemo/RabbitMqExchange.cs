using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqExchange
    {
        public static string DefaultExchange = String.Empty;

        public string Name { get; set; } = DefaultExchange;
        public string Type { get; set; } // direct / topic / fanout / header  (case sensitive)
        public bool Durable { get; set; }
        public byte MessageDeliveryMode { get; set; } = 1;  //1 = Non-Persistent  2 = Persistent
        public bool AutoDelete { get; set; }
        public bool Internal { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
        
    }
}
