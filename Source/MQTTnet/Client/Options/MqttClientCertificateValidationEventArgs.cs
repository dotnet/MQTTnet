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
        public X509Certificate Certificate { get; set; }

        public X509Chain Chain { get; set; }

        public SslPolicyErrors SslPolicyErrors { get; set; }

        public IMqttClientChannelOptions ClientOptions { get; set; }
    }
}
