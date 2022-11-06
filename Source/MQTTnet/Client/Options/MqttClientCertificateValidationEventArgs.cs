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
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            Chain = chain ?? throw new ArgumentNullException(nameof(chain));
            SslPolicyErrors = sslPolicyErrors;
            ClientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
        }

        public X509Certificate Certificate { get; }

        public X509Chain Chain { get; }

        public IMqttClientChannelOptions ClientOptions { get; }

        public SslPolicyErrors SslPolicyErrors { get; }
    }
}