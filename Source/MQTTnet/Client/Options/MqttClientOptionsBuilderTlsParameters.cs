// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Authentication;

namespace MQTTnet.Client
{
    [Obsolete("Use methods from MqttClientOptionsBuilder instead.")]
    public sealed class MqttClientOptionsBuilderTlsParameters
    {
        IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate> _obsoleteCertificates;

        public bool UseTls { get; set; }

        public Func<MqttClientCertificateValidationEventArgs, bool> CertificateValidationHandler { get; set; }

#if NET48 || NETCOREAPP3_1_OR_GREATER
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;
#else
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | (SslProtocols)0x00003000 /*Tls13*/;
#endif

#if WINDOWS_UWP
        public IEnumerable<IEnumerable<byte>> Certificates { get; set; }
#else
        [Obsolete("Use CertificatesProvider instead.")]
        public IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate> Certificates
        {
            get => _obsoleteCertificates;
            set
            {
                _obsoleteCertificates = value;

                if (value == null)
                {
                    CertificatesProvider = null;
                }
                else
                {
                    CertificatesProvider = new DefaultMqttCertificatesProvider(value);
                }
            }
        }
#endif

#if NETCOREAPP3_1_OR_GREATER
        public List<System.Net.Security.SslApplicationProtocol> ApplicationProtocols { get; set; }
#endif

        public bool AllowUntrustedCertificates { get; set; }

        public bool IgnoreCertificateChainErrors { get; set; }

        public bool IgnoreCertificateRevocationErrors { get; set; }

        public IMqttClientCertificatesProvider CertificatesProvider { get; set; }
    }
}