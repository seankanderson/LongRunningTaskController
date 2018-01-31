using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqExchange
    {
        public static string DefaultExchange = "";

        public string Name { get; set; } = String.Empty;
        public string Type { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public bool Internal { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
        
    }
}
