// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Security;
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

        public X509RevocationMode RevocationMode { get; set; } = X509RevocationMode.Online;

        /// <summary>
        ///     Gets or sets the provider for certificates.
        ///     This provider gets called whenever the client wants to connect
        ///     with the server and requires certificates for authentication.
        ///     The implementation may return different certificates each time.
        /// </summary>
        public IMqttClientCertificatesProvider ClientCertificatesProvider { get; set; }

#if NETCOREAPP3_1_OR_GREATER
        public List<SslApplicationProtocol> ApplicationProtocols { get; set; }

        public CipherSuitesPolicy CipherSuitesPolicy { get; set; }

        public EncryptionPolicy EncryptionPolicy { get; set; } = EncryptionPolicy.RequireEncryption;

        public bool AllowRenegotiation { get; set; } = true;
#endif

        /// <summary>
        ///     Gets or sets the target host.
        ///     If the value is null or empty the same host as the TCP socket host will be used.
        /// </summary>
        public string TargetHost { get; set; }

#if NET48 || NETCOREAPP3_1_OR_GREATER
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;

#else
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12 | (SslProtocols)0x00003000 /*Tls13*/;
#endif

#if NET7_0_OR_GREATER
        public X509Certificate2Collection TrustChain { get; set; }
#endif
    }
}