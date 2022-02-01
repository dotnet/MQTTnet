using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client
{
    public sealed class MqttClientTlsOptions
    {
        public Func<MqttClientCertificateValidationEventArgs, bool> CertificateValidationHandler { get; set; }

        public bool UseTls { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool AllowUntrustedCertificates { get; set; }

#if WINDOWS_UWP
        public List<byte[]> Certificates { get; set; }
#else
        public List<X509Certificate> Certificates { get; set; }
#endif

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        public List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get; set; }
#endif

        public SslProtocols SslProtocol { get; set; } = SslProtocols.None;
    }
}