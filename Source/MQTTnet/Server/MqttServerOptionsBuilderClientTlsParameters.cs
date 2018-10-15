using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Server
{
    public class MqttServerOptionsBuilderClientTlsParameters
    {
        public bool ClientCertificateRequired { get; set; }

        public Func<X509Certificate, X509Chain, SslPolicyErrors, bool> ClientCertificateValidationCallback
        {
            get;
            set;
        }

        public bool AllowUntrustedCertificates { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }
    }
}
