using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqTaskDemoParameters
    {
        public RabbitMqConnection Connection { get; set; }
        public string Certificate { get; set; }
        public RabbitMqQueue Queue { get; set; }
        public RabbitMqExchange Exchange { get; set; }
        public int NumberOfTasks { get; set; }
        public bool PublisherConfirmation { get; set; }
        public string Role { get; set; }
    }
}
