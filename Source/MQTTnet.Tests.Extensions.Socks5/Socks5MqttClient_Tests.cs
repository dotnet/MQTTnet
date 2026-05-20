// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Socks5;
using MQTTnet.Server;

namespace MQTTnet.Tests.Extensions.Socks5;

[TestClass]
public sealed class Socks5MqttClient_Tests
{
    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_No_Auth_PubSub_Works()
    {
        Socks5TestHelper.BrokerHandle broker = null;
        FakeSocks5Server proxy = null;
        IMqttClient client = null;

        try
        {
            broker = await Socks5TestHelper.StartBrokerAsync();
            var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

            proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

            client = new MqttClientFactory().CreateMqttClient();

            var receivedTopic = string.Empty;
            var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            client.ApplicationMessageReceivedAsync += e =>
            {
                receivedTopic = e.ApplicationMessage.Topic;
                received.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()));
                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", brokerPort)
                .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
                .Build();

            await client.ConnectAsync(options, CancellationToken.None);
            Assert.IsTrue(client.IsConnected);

            await client.SubscribeAsync("topic/socks5");
            await client.PublishStringAsync("topic/socks5", "hello-socks5");

            var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual("topic/socks5", receivedTopic);
            Assert.AreEqual("hello-socks5", payload);

            Assert.HasCount(1, proxy.RecordedConnects);
            Assert.AreEqual(brokerPort, proxy.RecordedConnects[0].Port);
        }
        finally
        {
            if (client != null)
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
                client.Dispose();
            }

            if (proxy != null)
            {
                await proxy.DisposeAsync();
            }

            if (broker != null)
            {
                await broker.Server.StopAsync();
                broker.Server.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_With_UsernamePassword_Works()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            AllowedMethods = [0x02],
            ExpectedUsername = "alice",
            ExpectedPassword = "s3cr3t"
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port).WithCredentials("alice", "s3cr3t"))
            .Build();

        try
        {
            await client.ConnectAsync(options, CancellationToken.None);
            Assert.IsTrue(client.IsConnected);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_With_Wrong_Credentials_Throws_AuthFailed()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            AllowedMethods = [0x02],
            ForceAuthFailure = true
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port).WithCredentials("alice", "wrong"))
            .Build();

        try
        {
            var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
            Assert.AreEqual(MqttProxyErrorCode.ProxyAuthFailed, ex.ErrorCode);
        }
        finally
        {
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Method_Rejected_Throws_AuthMethodRejected()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            AllowedMethods = [0x01] // GSSAPI which the client never offers.
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        try
        {
            var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
            Assert.AreEqual(MqttProxyErrorCode.ProxyAuthMethodRejected, ex.ErrorCode);
        }
        finally
        {
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Broker_Host_Unreachable_Maps_To_HostUnreachable()
    {
        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            ConnectReplyCode = 0x04
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.invalid", 1883)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyHostUnreachable, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Broker_Connection_Refused_Maps_To_ConnectionRefused()
    {
        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            ConnectReplyCode = 0x05
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", 1)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyConnectionRefused, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Proxy_Port_Closed_Throws_ProxyUnreachable()
    {
        // Bind a temporary listener just to grab a free port and immediately release it.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var freePort = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", 1883)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(freePort).WithHandshakeTimeout(TimeSpan.FromSeconds(5)))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyUnreachable, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Proxy_Host_Does_Not_Resolve_Throws_ProxyUnreachable()
    {
        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", 1883)
            .WithSocks5Proxy(p => p.WithHost("nonexistent.proxy.invalid").WithPort(1080).WithHandshakeTimeout(TimeSpan.FromSeconds(5)))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyUnreachable, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Remote_Dns_Enabled_Sends_DomainName_To_Proxy()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port).WithResolveDnsRemotely(true))
            .Build();

        try
        {
            await client.ConnectAsync(options);
            Assert.IsTrue(client.IsConnected);
            Assert.HasCount(1, proxy.RecordedConnects);
            Assert.AreEqual<byte>(0x03, proxy.RecordedConnects[0].AddressType);
            Assert.AreEqual("localhost", proxy.RecordedConnects[0].Host);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Remote_Dns_Disabled_Sends_Ipv4_To_Proxy()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", brokerPort)
            .WithSocks5Proxy(p => p
                .WithHost("127.0.0.1")
                .WithPort(proxy.Port)
                .WithResolveDnsRemotely(false)
                .WithAddressFamily(AddressFamily.InterNetwork))
            .Build();

        try
        {
            await client.ConnectAsync(options);
            Assert.IsTrue(client.IsConnected);
            Assert.HasCount(1, proxy.RecordedConnects);
            Assert.AreEqual<byte>(0x01, proxy.RecordedConnects[0].AddressType);
            Assert.AreEqual("127.0.0.1", proxy.RecordedConnects[0].Host);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Ipv6_IpEndPoint_Sends_Ipv6_AddressType()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

        using var client = new MqttClientFactory().CreateMqttClient();

        // Loopback (::1) is reachable on practically every CI runner.
        var options = new MqttClientOptionsBuilder()
            .WithEndPoint(new IPEndPoint(IPAddress.IPv6Loopback, brokerPort))
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        try
        {
            await client.ConnectAsync(options);
            Assert.IsTrue(client.IsConnected);
            Assert.HasCount(1, proxy.RecordedConnects);
            Assert.AreEqual<byte>(0x04, proxy.RecordedConnects[0].AddressType);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Cancellation_During_Handshake_Throws_OperationCanceled()
    {
        // Proxy that accepts the TCP connection but never replies to the greeting.
        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            DropAfterAccept = true
        });

        using var client = new MqttClientFactory().CreateMqttClient();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", 1883)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.ConnectAsync(options, cts.Token));
    }

    [TestMethod]
    public async Task Greeting_Returns_Wrong_Version_Throws_ProtocolError()
    {
        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            GreetingResponseOverride = [0x04, 0x00],
            DropAfterGreetingResponse = true
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", 1883)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyProtocolError, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Reconnect_After_Proxy_Disconnect_Works()
    {
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        try
        {
            await client.ConnectAsync(options);
            Assert.IsTrue(client.IsConnected);

            // Force the broker side to drop the client by stopping & restarting it (proxy
            // forwarding closes when the upstream connection closes).
            await broker.Server.StopAsync();
            broker.Server.Dispose();

            await Task.Delay(500);

            broker = await Socks5TestHelper.StartBrokerAsync();
            var newPort = Socks5TestHelper.GetBrokerPort(broker);

            options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", newPort)
                .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
                .Build();

            await client.ConnectAsync(options);
            Assert.IsTrue(client.IsConnected);

            // Two distinct CONNECT requests should have hit the proxy.
            Assert.IsGreaterThanOrEqualTo(2, proxy.RecordedConnects.Count);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Connect_Tls_Via_Socks5_PubSub_Works()
    {
        var tlsBroker = await Socks5TestHelper.StartTlsBrokerAsync();
        try
        {
            using (tlsBroker.Certificate)
            {
                var brokerPort = Socks5TestHelper.GetTlsBrokerPort(tlsBroker);

                await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions());

                using var client = new MqttClientFactory().CreateMqttClient();

                var receivedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                client.ApplicationMessageReceivedAsync += e =>
                {
                    receivedTcs.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray()));
                    return Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", brokerPort)
                    .WithTlsOptions(o => o
                        .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12)
                        .WithCertificateValidationHandler(_ => true))
                    .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
                    .Build();

                await client.ConnectAsync(options);
                Assert.IsTrue(client.IsConnected);

                await client.SubscribeAsync("tls/socks5");
                await client.PublishStringAsync("tls/socks5", "tls-hello");

                var payload = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.AreEqual("tls-hello", payload);

                // Validate that the proxy saw a CONNECT with domain forwarding.
                Assert.HasCount(1, proxy.RecordedConnects);
                Assert.AreEqual<byte>(0x03, proxy.RecordedConnects[0].AddressType);
                Assert.AreEqual("localhost", proxy.RecordedConnects[0].Host);

                try { await client.DisconnectAsync(); } catch { /* ignore */ }
            }
        }
        finally
        {
            await tlsBroker.Server.StopAsync();
            tlsBroker.Server.Dispose();
        }
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_When_Command_Not_Supported_Maps_To_CommandNotSupported()
    {
        // Server replies with REP=0x07 ("Command not supported / protocol error") per RFC 1928 §6.
        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            ConnectReplyCode = 0x07
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("broker.example", 1883)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        var ex = await Assert.ThrowsExactlyAsync<MqttProxyException>(() => client.ConnectAsync(options));
        Assert.AreEqual(MqttProxyErrorCode.ProxyCommandNotSupported, ex.ErrorCode);
    }

    [TestMethod]
    public async Task Connect_Tcp_Via_Socks5_Parses_DomainName_BindAddress_In_Reply()
    {
        // RFC 1928 §6 allows ATYP=0x03 (DOMAINNAME) in the CONNECT reply with a variable-length
        // BND.ADDR. Verify the client correctly parses the length-prefixed bind address and
        // completes the handshake even when the server uses a non-IP BND.ADDR.
        var broker = await Socks5TestHelper.StartBrokerAsync();
        var brokerPort = Socks5TestHelper.GetBrokerPort(broker);

        await using var proxy = new FakeSocks5Server(new FakeSocks5ServerOptions
        {
            ReplyAddressType = 0x03,
            ReplyBindHost = "proxy.example.internal",
            ReplyBindPort = 4242
        });

        using var client = new MqttClientFactory().CreateMqttClient();

        var connectOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("127.0.0.1", brokerPort)
            .WithSocks5Proxy(p => p.WithHost("127.0.0.1").WithPort(proxy.Port))
            .Build();

        try
        {
            await client.ConnectAsync(connectOptions);
            Assert.IsTrue(client.IsConnected);
        }
        finally
        {
            try { await client.DisconnectAsync(); } catch { /* ignore */ }
            await broker.Server.StopAsync();
            broker.Server.Dispose();
        }
    }
}
