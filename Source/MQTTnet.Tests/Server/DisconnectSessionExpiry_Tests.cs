// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class DisconnectSessionExpiry_Tests : BaseTestClass
{
    [TestMethod]
    [DataRow(0U, 0, false)]
    [DataRow(1U, 2100, false)]
    [DataRow(60U, 0, true)]
    [DataRow(uint.MaxValue, 0, true)]
    public async Task Disconnect_Replaces_Connect_Expiry(uint interval, int delay, bool expectedSessionPresent)
    {
        using var environment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnectedAsync += _ =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        };
        var client = await environment.ConnectClient(o => o.WithClientId("expiry").WithCleanSession(false).WithSessionExpiryInterval(30));
        await client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithSessionExpiryInterval(interval).Build());
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var sessions = await server.GetSessionsAsync();
        if (interval == 0)
        {
            Assert.IsEmpty(sessions);
        }
        else
        {
            Assert.AreEqual(interval, sessions.Single().ExpiryInterval);
        }

        await Task.Delay(delay);

        var result = await client.ConnectAsync(client.Options);
        Assert.AreEqual(expectedSessionPresent, result.IsSessionPresent);
    }

    [TestMethod]
    [DataRow(0U, 0)]
    [DataRow(1U, 2100)]
    public async Task Resumed_Session_Uses_Latest_Connect_Expiry(uint interval, int delay)
    {
        using var environment = CreateTestEnvironment(MqttProtocolVersion.V500);
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnectedAsync += _ =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        };
        var client = await environment.ConnectClient(o => o.WithClientId("expiry").WithCleanSession(false).WithSessionExpiryInterval(30));
        await client.DisconnectAsync();
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));

        client.Options.SessionExpiryInterval = interval;
        var resumed = await client.ConnectAsync(client.Options);
        Assert.IsTrue(resumed.IsSessionPresent);
        disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await client.DisconnectAsync();
        await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(delay);

        var result = await client.ConnectAsync(client.Options);
        Assert.IsFalse(result.IsSessionPresent);
    }

    [TestMethod]
    public void Explicit_Zero_Survives_Disconnect_Roundtrip()
    {
        byte[] bytes = [0xE0, 7, 0, 5, 0x11, 0, 0, 0, 0];
        var packet = (MqttDisconnectPacket)MqttPacketSerializationHelper.DecodePacket(bytes, MqttProtocolVersion.V500);
        var encoded = MqttPacketSerializationHelper.EncodePacket(packet, MqttProtocolVersion.V500);

        CollectionAssert.AreEqual(bytes, encoded);
    }

    [TestMethod]
    public void Default_Disconnect_Omits_Expiry()
    {
        var options = new MqttClientDisconnectOptionsBuilder().Build();
        Assert.IsFalse(options.HasSessionExpiryInterval);
        Assert.IsFalse(MqttDisconnectPacketFactory.Create(options).HasSessionExpiryInterval);
        Assert.IsFalse(MqttDisconnectPacketFactory.Create(null).HasSessionExpiryInterval);

        byte[] bytes = [0xE0, 2, 0, 0];
        CollectionAssert.AreEqual(bytes, MqttPacketSerializationHelper.EncodePacket(MqttDisconnectPacketFactory.Create(options), MqttProtocolVersion.V500));
    }

    [TestMethod]
    public void Optional_Exception_Expiry_Preserves_Absence()
    {
        Assert.IsNull(new MqttClientUnexpectedDisconnectReceivedException(new MqttDisconnectPacket()).SessionExpiryInterval);
        Assert.AreEqual(0U, new MqttClientUnexpectedDisconnectReceivedException(new MqttDisconnectPacket { SessionExpiryInterval = 0 }).SessionExpiryInterval);
    }

    [TestMethod]
    public void Explicit_Zero_Options_Include_Expiry()
    {
        var options = new MqttClientDisconnectOptionsBuilder().WithSessionExpiryInterval(0).Build();
        Assert.IsTrue(options.HasSessionExpiryInterval);
        var packet = MqttDisconnectPacketFactory.Create(options);
        Assert.IsTrue(packet.HasSessionExpiryInterval);
        byte[] bytes = [0xE0, 7, 0, 5, 0x11, 0, 0, 0, 0];
        CollectionAssert.AreEqual(bytes, MqttPacketSerializationHelper.EncodePacket(packet, MqttProtocolVersion.V500));
    }

    [TestMethod]
    public void Duplicate_Expiry_Is_Rejected_Even_When_First_Is_Zero()
    {
        byte[] bytes = [0xE0, 12, 0, 10, 0x11, 0, 0, 0, 0, 0x11, 0, 0, 0, 1];
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.DecodePacket(bytes, MqttProtocolVersion.V500));
    }

    [TestMethod]
    public async Task Zero_Connect_Expiry_Cannot_Be_Extended_On_Disconnect()
    {
        using var environment = new TestEnvironment();
        var server = await environment.StartServer(o => o.WithPersistentSessions());
        using var client = environment.CreateLowLevelClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var options = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", environment.ServerPort)
            .WithProtocolVersion(MqttProtocolVersion.V500).WithClientId("expiry").Build();
        await client.ConnectAsync(options, timeout.Token);
        await client.SendAsync(MqttConnectPacketFactory.Create(options), timeout.Token);
        Assert.IsInstanceOfType<MqttConnAckPacket>(await client.ReceiveAsync(timeout.Token));

        await client.SendAsync(new MqttDisconnectPacket { SessionExpiryInterval = 1 }, timeout.Token);
        var response = await client.ReceiveAsync(timeout.Token);
        Assert.IsInstanceOfType<MqttDisconnectPacket>(response);
        Assert.AreEqual(MqttDisconnectReasonCode.ProtocolError, ((MqttDisconnectPacket)response).ReasonCode);
        Assert.IsFalse(((MqttDisconnectPacket)response).HasSessionExpiryInterval);
    }
}