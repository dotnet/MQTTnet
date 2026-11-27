// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class ClientNegativePubRec_Tests : BaseTestClass
{
    [TestMethod]
    [DataRow(MqttPubRecReasonCode.UnspecifiedError)]
    [DataRow(MqttPubRecReasonCode.ImplementationSpecificError)]
    [DataRow(MqttPubRecReasonCode.NotAuthorized)]
    [DataRow(MqttPubRecReasonCode.TopicNameInvalid)]
    [DataRow(MqttPubRecReasonCode.PacketIdentifierInUse)]
    [DataRow(MqttPubRecReasonCode.QuotaExceeded)]
    [DataRow(MqttPubRecReasonCode.PayloadFormatInvalid)]
    public async Task Negative_PubRec_Completes_With_Original_Properties_Without_PubRel(MqttPubRecReasonCode reason)
    {
        using var environment = new TestEnvironment(TestContext, MqttProtocolVersion.V500);
        var server = await environment.StartServer();
        ushort identifier = 0;
        server.InterceptingOutboundPacketAsync += e =>
        {
            if (e.Packet is MqttPubRecPacket packet)
            {
                identifier = packet.PacketIdentifier;
                packet.ReasonCode = reason;
                packet.ReasonString = "original rejection";
                packet.UserProperties = [new MqttUserProperty("same", new ReadOnlyMemory<byte>("first"u8.ToArray())), new MqttUserProperty("same", new ReadOnlyMemory<byte>("second"u8.ToArray()))];
            }
            return Task.CompletedTask;
        };
        var client = environment.CreateClient();
        var pubRels = 0;
        client.InspectPacketAsync += e =>
        {
            if (e.Direction == MqttPacketFlowDirection.Outbound && e.Buffer.Length > 0 && e.Buffer[0] >> 4 == 6)
                Interlocked.Increment(ref pubRels);
            return Task.CompletedTask;
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", environment.ServerPort)
            .WithProtocolVersion(MqttProtocolVersion.V500).Build(), timeout.Token);
        // Two publishes also prove a rejected exchange does not strand the next request.
        for (var index = 0; index < 2; index++)
        {
            var result = await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("rejected")
                .WithPayload($"message-{index}").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build(), timeout.Token);
            Assert.AreEqual(identifier, result.PacketIdentifier);
            Assert.AreEqual((MqttClientPublishReasonCode)(int)reason, result.ReasonCode);
            Assert.AreEqual("original rejection", result.ReasonString);
            Assert.HasCount(2, result.UserProperties);
            Assert.AreEqual("same", result.UserProperties.ElementAt(0).Name);
            Assert.AreEqual("first", Encoding.UTF8.GetString(result.UserProperties.ElementAt(0).ValueBuffer.Span));
            Assert.AreEqual("same", result.UserProperties.ElementAt(1).Name);
            Assert.AreEqual("second", Encoding.UTF8.GetString(result.UserProperties.ElementAt(1).ValueBuffer.Span));
        }
        Assert.AreEqual(0, pubRels);
        Assert.IsTrue(client.IsConnected);
        environment.ThrowIfLogErrors();
    }

    [TestMethod]
    [DataRow(MqttProtocolVersion.V311, MqttPubRecReasonCode.Success)]
    [DataRow(MqttProtocolVersion.V311, MqttPubRecReasonCode.UnspecifiedError)]
    [DataRow(MqttProtocolVersion.V500, MqttPubRecReasonCode.Success)]
    [DataRow(MqttProtocolVersion.V500, MqttPubRecReasonCode.NoMatchingSubscribers)]
    public async Task Nonnegative_Wire_PubRec_Still_Sends_PubRel(MqttProtocolVersion protocol, MqttPubRecReasonCode reason)
    {
        using var environment = new TestEnvironment(TestContext, protocol);
        var server = await environment.StartServer();
        server.InterceptingOutboundPacketAsync += e =>
        {
            if (e.Packet is MqttPubRecPacket packet) packet.ReasonCode = reason;
            return Task.CompletedTask;
        };
        var client = environment.CreateClient();
        var pubRels = 0;
        client.InspectPacketAsync += e =>
        {
            if (e.Direction == MqttPacketFlowDirection.Outbound && e.Buffer.Length > 0 && e.Buffer[0] >> 4 == 6)
                Interlocked.Increment(ref pubRels);
            return Task.CompletedTask;
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", environment.ServerPort)
            .WithProtocolVersion(protocol).Build(), timeout.Token);
        var result = await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("success")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build(), timeout.Token);
        Assert.AreEqual(protocol == MqttProtocolVersion.V311 ? MqttClientPublishReasonCode.Success : (MqttClientPublishReasonCode)(int)reason, result.ReasonCode);
        Assert.AreEqual(1, pubRels);
        environment.ThrowIfLogErrors();
    }

    [TestMethod]
    public void Negative_PubRec_Result_Does_Not_Require_PubComp_And_Wins_Over_Obsolete_PubComp()
    {
        var pubRec = new MqttPubRecPacket { PacketIdentifier = 42, ReasonCode = MqttPubRecReasonCode.NotAuthorized, ReasonString = "denied" };
        foreach (var pubComp in new MqttPubCompPacket[] { null, new() { PacketIdentifier = 42, ReasonCode = MqttPubCompReasonCode.PacketIdentifierNotFound } })
        {
            var result = MqttClientPublishResultFactory.Create(pubRec, pubComp);
            Assert.AreEqual((ushort)42, result.PacketIdentifier);
            Assert.AreEqual(MqttClientPublishReasonCode.NotAuthorized, result.ReasonCode);
            Assert.AreEqual("denied", result.ReasonString);
            Assert.IsEmpty(result.UserProperties);
        }
    }

    [TestMethod]
    public async Task Duplicate_Negative_PubRec_Without_Awaiter_Does_Not_Send_PubRel()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = timeout.Token;
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var pubRels = 0;
        var peer = RunPeer();
        using var client = new MqttClientFactory().CreateMqttClient();
        var receivedPubRecs = 0;
        client.InspectPacketAsync += e =>
        {
            if (e.Direction == MqttPacketFlowDirection.Inbound && e.Buffer.Length > 0 && e.Buffer[0] >> 4 == 5)
                Interlocked.Increment(ref receivedPubRecs);
            return Task.CompletedTask;
        };
        try
        {
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", port)
                .WithProtocolVersion(MqttProtocolVersion.V500).Build(), token);
            var result = await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("negative")
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build(), token);
            await client.PingAsync(token);
            await peer;
            Assert.AreEqual(MqttClientPublishReasonCode.ImplementationSpecificError, result.ReasonCode);
            Assert.AreEqual(2, receivedPubRecs);
            Assert.AreEqual(0, pubRels);
        }
        finally
        {
            await timeout.CancelAsync();
            try { await peer; } catch (OperationCanceledException) { }
        }

        async Task RunPeer()
        {
            using var socket = await listener.AcceptTcpClientAsync(token);
            using var stream = socket.GetStream();
            Assert.AreEqual(1, (await ReadPacket()).Type);
            await stream.WriteAsync(new byte[] { 0x20, 3, 0, 0, 0 }, token);
            var publish = await ReadPacket();
            Assert.AreEqual(3, publish.Type);
            var identifierOffset = 2 + (publish.Body[0] << 8) + publish.Body[1];
            var high = publish.Body[identifierOffset];
            var low = publish.Body[identifierOffset + 1];
            // Two complete negative PUBRECs: one belongs to the request, the second is late.
            await stream.WriteAsync(new byte[] { 0x50, 4, high, low, 0x83, 0, 0x50, 4, high, low, 0x83, 0 }, token);
            while (true)
            {
                var packet = await ReadPacket();
                if (packet.Type == 6)
                {
                    pubRels++;
                    // Keep a broken client live long enough to reach the assertions.
                    await stream.WriteAsync(new byte[] { 0x70, 4, high, low, 0x92, 0 }, token);
                }
                else
                {
                    Assert.AreEqual(12, packet.Type);
                    await stream.WriteAsync(new byte[] { 0xd0, 0 }, token);
                    return;
                }
            }

            async Task<(int Type, byte[] Body)> ReadPacket()
            {
                var value = new byte[1];
                await stream.ReadExactlyAsync(value, token);
                var type = value[0] >> 4;
                var length = 0;
                var multiplier = 1;
                do
                {
                    await stream.ReadExactlyAsync(value, token);
                    length += (value[0] & 127) * multiplier;
                    multiplier *= 128;
                    if (multiplier > 128 * 128 * 128 * 128) throw new InvalidOperationException("Invalid packet length");
                } while ((value[0] & 128) != 0);
                var body = new byte[length];
                await stream.ReadExactlyAsync(body, token);
                return (type, body);
            }
        }
    }
}
