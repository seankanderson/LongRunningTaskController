using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqConnection
    {
        public string Host { get; set; }
        public int Port { get; set; } = 5672;
    }
}
