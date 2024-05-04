// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client
{
    public sealed class MqttClientCertificateValidationEventArgs : EventArgs
    {
        public MqttClientCertificateValidationEventArgs(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, IMqttClientChannelOptions clientOptions)
        {
            Certificate = certificate;
            Chain = chain;
            SslPolicyErrors = sslPolicyErrors;
            ClientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
        }

        public X509Certificate Certificate { get; }

        public X509Chain Chain { get; }

        public IMqttClientChannelOptions ClientOptions { get; }

#if NET452 || NET461 || NET48
        /// <summary>
        ///     Can be a host string name or an object derived from WebRequest.
        /// </summary>
        public object Sender { get; set; }
#endif

        public SslPolicyErrors SslPolicyErrors { get; }
    }
}