// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Certificates;
using MQTTnet.Server;

namespace MQTTnet.Tests.Extensions.Socks5;

internal static class Socks5TestHelper
{
    public static async Task<BrokerHandle> StartBrokerAsync()
    {
        var factory = new MqttServerFactory();
        var options = factory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(0)
            .Build();

        var server = factory.CreateMqttServer(options);
        await server.StartAsync();
        return new BrokerHandle(server, options);
    }

    public static int GetBrokerPort(BrokerHandle handle)
    {
        return handle.Options.DefaultEndpointOptions.Port;
    }

    public static async Task<TlsBrokerHandle> StartTlsBrokerAsync()
    {
        var certificate = CreateSelfSignedServerCertificate();

        var factory = new MqttServerFactory();
        var options = factory.CreateServerOptionsBuilder()
            .WithoutDefaultEndpoint()
            .WithEncryptedEndpoint()
            .WithEncryptedEndpointPort(0)
            .WithEncryptionCertificate(new InMemoryCertificateProvider(certificate))
            .Build();

        var server = factory.CreateMqttServer(options);
        await server.StartAsync();
        return new TlsBrokerHandle(server, options, certificate);
    }

    public static int GetTlsBrokerPort(TlsBrokerHandle handle)
    {
        return handle.Options.TlsEndpointOptions.Port;
    }

    public static X509Certificate2 CreateSelfSignedServerCertificate()
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");

        using var rsa = RSA.Create(2048);
        var certRequest = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certRequest.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
        certRequest.CertificateExtensions.Add(sanBuilder.Build());

        using var cert = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddMinutes(60));
#pragma warning disable SYSLIB0057
        return new X509Certificate2(
            cert.Export(X509ContentType.Pfx),
            (string)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
    }

    internal sealed record BrokerHandle(MqttServer Server, MqttServerOptions Options);

    internal sealed record TlsBrokerHandle(MqttServer Server, MqttServerOptions Options, X509Certificate2 Certificate);

    sealed class InMemoryCertificateProvider : ICertificateProvider
    {
        readonly X509Certificate2 _certificate;

        public InMemoryCertificateProvider(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public X509Certificate2 GetCertificate() => _certificate;
    }
}
