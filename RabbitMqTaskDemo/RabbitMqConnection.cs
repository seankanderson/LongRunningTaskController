using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqTaskDemo
{
    public class RabbitMqConnection
    {
        public string HostName { get; set; }
        public int Port { get; set; } = 5672;
        public string VHost { get; set; } = "/";
        public string User { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;

        public bool PublisherConfirmation { get; set; }

        public string CertPath { get; set; } = "WindowsCertStore";
        public string CertPassphrase { get; set; } = String.Empty;
        public string CertServerName { get; set; } = String.Empty;
        public bool TlsEnabled { get; set; } = false;
    }
}
