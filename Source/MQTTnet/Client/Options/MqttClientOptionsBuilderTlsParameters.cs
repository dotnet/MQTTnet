using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client
{
    public class MqttClientOptionsBuilderTlsParameters
    {
        public bool UseTls { get; set; }
        
        public Func<MqttClientCertificateValidationEventArgs, bool> CertificateValidationHandler { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

#if WINDOWS_UWP
        public IEnumerable<IEnumerable<byte>> Certificates { get; set; }
#else
        public IEnumerable<X509Certificate> Certificates { get; set; }
#endif

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
        public List<SslApplicationProtocol> ApplicationProtocols { get;set; }
#endif

	    public bool AllowUntrustedCertificates { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }
    }
}
