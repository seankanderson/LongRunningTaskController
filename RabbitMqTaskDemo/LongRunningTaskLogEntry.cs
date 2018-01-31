using System;
using System.Collections.Generic;

namespace RabbitMqTaskDemo
{
    public class LongRunningTaskLogEntry
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;        
        public long ProcessingTimeInMs { get; set; }
        public bool IsException { get; set; }
        public Dictionary<string, string> Details { get; } = new Dictionary<string, string>();
    }
}
