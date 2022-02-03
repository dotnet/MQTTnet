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

#if WINDOWS_UWP
        public List<byte[]> Certificates { get; set; }
#else
        public List<X509Certificate> Certificates { get; set; }
#endif

#if NETCOREAPP3_1 || NET5_0
        public List<SslApplicationProtocol> ApplicationProtocols { get; set; }
#endif

#if NET452 || NET46 || NET461 || NET462 || NET47
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
#else
        public SslProtocols SslProtocol { get; set; } = SslProtocols.None;
#endif

        [Obsolete("This property will be removed soon. Use CertificateValidationHandler instead.")]
        public Func<X509Certificate, X509Chain, SslPolicyErrors, IMqttClientOptions, bool> CertificateValidationCallback { get; set; }

        public Func<MqttClientCertificateValidationCallbackContext, bool> CertificateValidationHandler { get; set; }
    }
}
