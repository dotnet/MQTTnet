using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client.Options
{
    public class MqttClientTlsOptions
    {
        public bool UseTls { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool AllowUntrustedCertificates { get; set; }

        public List<X509Certificate> Certificates { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;

        public Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> CertificateValidationCallback { get; set; }
    }
}
