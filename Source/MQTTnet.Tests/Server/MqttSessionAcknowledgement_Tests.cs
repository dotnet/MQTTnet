// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class MqttSessionAcknowledgement_Tests : IDisposable
{
    MqttClientSessionsManager _sessionsManager;
    MqttRetainedMessagesManager _retainedMessagesManager;
    MqttSession _session;

    [TestInitialize]
    public void Initialize()
    {
        var options = new MqttServerOptions();
        var events = new MqttServerEventContainer();
        var logger = MqttNetNullLogger.Instance;
        _retainedMessagesManager = new MqttRetainedMessagesManager(events, logger);
        _sessionsManager = new MqttClientSessionsManager(options, _retainedMessagesManager, events, logger);
        _session = new MqttSession(new MqttConnectPacket { ClientId = "acknowledgement-test" }, new Hashtable(), options, events,
            _retainedMessagesManager, _sessionsManager);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _sessionsManager?.Dispose();
        _retainedMessagesManager?.Dispose();
    }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public void Legacy_Overload_Preserves_Identifier_Only_Consumption(MqttQualityOfServiceLevel qos)
    {
        // Method-group binding also checks source compatibility of the original signature.
        Func<ushort, MqttPublishPacket> acknowledge = _session.AcknowledgePublishPacket;
        var first = Track(qos);
        var second = Track(qos);

        Assert.IsNull(acknowledge(ushort.MaxValue));
        Assert.AreSame(first, _session.PeekAcknowledgePublishPacket(first.PacketIdentifier));
        Assert.AreSame(first, acknowledge(first.PacketIdentifier));
        Assert.IsNull(acknowledge(first.PacketIdentifier));
        Assert.AreSame(second, _session.PeekAcknowledgePublishPacket(second.PacketIdentifier));
        Assert.AreSame(second, acknowledge(second.PacketIdentifier));
    }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public void Qos_Aware_Overload_Rejects_Unknown_And_Wrong_Type_Acknowledgements(MqttQualityOfServiceLevel qos)
    {
        var packet = Track(qos);
        var wrongQos = qos == MqttQualityOfServiceLevel.AtLeastOnce ? MqttQualityOfServiceLevel.ExactlyOnce : MqttQualityOfServiceLevel.AtLeastOnce;

        Assert.IsNull(_session.AcknowledgePublishPacket(ushort.MaxValue, qos));
        Assert.IsNull(_session.AcknowledgePublishPacket(packet.PacketIdentifier, wrongQos));
        Assert.AreSame(packet, _session.PeekAcknowledgePublishPacket(packet.PacketIdentifier));
        Assert.AreSame(packet, _session.AcknowledgePublishPacket(packet.PacketIdentifier, qos));
        Assert.IsNull(_session.AcknowledgePublishPacket(packet.PacketIdentifier, qos));
        Assert.IsNull(_session.AcknowledgePublishPacket(packet.PacketIdentifier));
    }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
    public void Both_Overloads_Consume_An_Exchange_Only_Once(MqttQualityOfServiceLevel qos)
    {
        var packet = Track(qos);
        var other = Track(qos);
        var acknowledged = 0;

        Parallel.For(0, 32, index =>
        {
            var result = index % 2 == 0
                ? _session.AcknowledgePublishPacket(packet.PacketIdentifier)
                : _session.AcknowledgePublishPacket(packet.PacketIdentifier, qos);
            if (result != null)
            {
                Interlocked.Increment(ref acknowledged);
            }
        });

        Assert.AreEqual(1, acknowledged);
        Assert.IsNull(_session.PeekAcknowledgePublishPacket(packet.PacketIdentifier));
        Assert.AreSame(other, _session.AcknowledgePublishPacket(other.PacketIdentifier));
    }

    MqttPublishPacket Track(MqttQualityOfServiceLevel qos)
    {
        var packet = new MqttPublishPacket { Topic = "acknowledgement-test", QualityOfServiceLevel = qos };
        _session.EnqueueDataPacket(new MqttPacketBusItem(packet));
        // Simulate taking the packet for transmission while leaving its acknowledgement outstanding.
        _session.DequeuePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
        return packet;
    }
}
