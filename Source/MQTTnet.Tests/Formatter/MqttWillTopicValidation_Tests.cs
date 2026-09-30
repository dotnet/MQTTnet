// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Formatter;

[TestClass]
public sealed class MqttWillTopicValidation_Tests : BaseTestClass
{
    [TestMethod]
    [DataRow("")]
    [DataRow("23")]
    [DataRow("612F2B")]
    [DataRow("610062")]
    [DataRow("C080")]
    [DataRow("EDA080")]
    [DataRow("FF")]
    [DataRow("E282")]
    public void Reject_Invalid_Will_Topic(string topicHex)
    {
        foreach (var version in new[] { MqttProtocolVersion.V311, MqttProtocolVersion.V500 })
        {
            var packet = CreateConnectPacket(version, Convert.FromHexString(topicHex));
            Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.DecodePacket(packet, version));
        }
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("/")]
    [DataRow("a//b")]
    [DataRow("$status/a")]
    [DataRow("温度/😀")]
    [DataRow("\uFEFFa\uFFFD")]
    public void Accept_Valid_Will_Topic(string topic)
    {
        foreach (var version in new[] { MqttProtocolVersion.V311, MqttProtocolVersion.V500 })
        {
            var packet = CreateConnectPacket(version, Encoding.UTF8.GetBytes(topic));
            var decoded = (MqttConnectPacket)MqttPacketSerializationHelper.DecodePacket(packet, version);
            Assert.AreEqual(topic, decoded.WillTopic);
        }
    }

    [TestMethod]
    [DataRow(MqttProtocolVersion.V311)]
    [DataRow(MqttProtocolVersion.V500)]
    public async Task Server_Rejects_Wildcard_Will_Before_Accepting_Connection(MqttProtocolVersion version)
    {
        using var environment = new MQTTnet.Tests.Mockups.TestEnvironment();
        environment.IgnoreServerLogErrors = true;
        var server = await environment.StartServer();
        var accepted = false;
        server.ClientConnectedAsync += _ =>
        {
            accepted = true;
            return Task.CompletedTask;
        };

        using var client = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.ConnectAsync(IPAddress.Loopback, environment.ServerPort, timeout.Token);
        using var stream = client.GetStream();
        await stream.WriteAsync(CreateConnectPacket(version, "a/#"u8.ToArray()), timeout.Token);
        var response = new byte[128];
        var count = await stream.ReadAsync(response, timeout.Token);

        Assert.AreEqual(0, count);
        Assert.IsFalse(accepted);
    }

    static byte[] CreateConnectPacket(MqttProtocolVersion version, byte[] topic)
    {
        byte[] prefix = version == MqttProtocolVersion.V500
            ? [0, 4, 77, 81, 84, 84, 5, 6, 0, 0, 0, 0, 1, 97, 0]
            : [0, 4, 77, 81, 84, 84, 4, 6, 0, 0, 0, 1, 97];
        byte[] body = [.. prefix, 0, (byte)topic.Length, .. topic, 0, 0];
        return [0x10, (byte)body.Length, .. body];
    }
}