// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

using MQTTnet.Extensions.Socks5;
using MQTTnet.Samples.Helpers;

namespace MQTTnet.Samples.Client;

public static class Client_Socks5_Samples
{
    public static async Task Connect_Tcp_Via_Socks5_No_Auth()
    {
        /*
         * This sample connects to an MQTT broker through a SOCKS5 proxy that does not
         * require authentication. The SOCKS5 stream provider performs the proxy handshake
         * (RFC 1928) and returns a connected stream that the MQTT TCP channel can use as if
         * it were a direct TCP connection.
         *
         * Host name resolution is performed by the proxy by default (ATYP=DOMAINNAME), which
         * is the desired behavior when the MQTT broker hostname is only reachable from inside
         * the network the proxy lives in.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.internal.example", 1883)
            .WithSocks5Proxy(socks => socks
                .WithHost("proxy.example.com")
                .WithPort(1080))
            .Build();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

        Console.WriteLine("The MQTT client is connected through SOCKS5.");
        response.DumpToConsole();

        var disconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
        await mqttClient.DisconnectAsync(disconnectOptions, CancellationToken.None);
    }

    public static async Task Connect_Tcp_With_Tls_Via_Socks5_With_Credentials()
    {
        /*
         * This sample connects to a TLS-protected MQTT broker through a SOCKS5 proxy that
         * requires username/password authentication (RFC 1929).
         *
         * Note that the TLS handshake is layered on top of the SOCKS5 tunnel: the SNI used
         * by SslStream is derived from the broker hostname (or the value passed to
         * WithTargetHost), not from the proxy hostname.
         */

        var mqttFactory = new MqttClientFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.internal.example", 8883)
            .WithTlsOptions(tls => tls
                .WithTargetHost("broker.internal.example"))
            .WithSocks5Proxy(socks => socks
                .WithHost("proxy.example.com")
                .WithPort(1080)
                .WithCredentials("alice", "s3cr3t")
                .WithResolveDnsRemotely(true)
                .WithHandshakeTimeout(TimeSpan.FromSeconds(10)))
            .Build();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = await mqttClient.ConnectAsync(mqttClientOptions, timeout.Token);

        Console.WriteLine("The MQTT client is connected through SOCKS5 with TLS.");
        response.DumpToConsole();

        var disconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
        await mqttClient.DisconnectAsync(disconnectOptions, CancellationToken.None);
    }
}
