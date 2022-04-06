// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using MQTTnet.Certificates;

namespace MQTTnet.Server
{
    public sealed class MqttServerTlsTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
    {
        public MqttServerTlsTcpEndpointOptions()
        {
            Port = 8883;
        }

#if !WINDOWS_UWP
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
#endif
        public ICertificateProvider CertificateProvider { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public bool CheckCertificateRevocation { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
        
#if NETCOREAPP3_0_OR_GREATER
        public System.Net.Security.CipherSuitesPolicy CipherSuitesPolicy { get; set; }
#endif
    }
}
