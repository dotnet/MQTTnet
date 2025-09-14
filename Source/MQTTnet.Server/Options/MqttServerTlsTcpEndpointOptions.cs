// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using MQTTnet.Certificates;

namespace MQTTnet.Server;

public sealed class MqttServerTlsTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
{
    public MqttServerTlsTcpEndpointOptions()
    {
        Port = 8883;
    }

    public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

    public ICertificateProvider CertificateProvider { get; set; }

    public bool ClientCertificateRequired { get; set; }

    public bool CheckCertificateRevocation { get; set; }

    /// <summary>
    /// The default value is SslProtocols.None, which allows the operating system to choose the best protocol to use, and to block protocols that are not secure.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols">SslProtocols</seealso>
    public SslProtocols SslProtocol { get; set; } = SslProtocols.None;

    public System.Net.Security.CipherSuitesPolicy CipherSuitesPolicy { get; set; }
}