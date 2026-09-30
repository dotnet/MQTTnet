// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Exceptions;
using MQTTnet.Server.Internal;

namespace MQTTnet.Tests.Server;

[TestClass]
public sealed class EvictedPublishTracking_Tests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow(MqttQualityOfServiceLevel.AtMostOnce, false)]
    [DataRow(MqttQualityOfServiceLevel.AtMostOnce, true)]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce, false)]
    [DataRow(MqttQualityOfServiceLevel.AtLeastOnce, true)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce, false)]
    [DataRow(MqttQualityOfServiceLevel.ExactlyOnce, true)]
    public async Task Eviction_Removes_Only_The_Queued_Packet_From_Recovery(MqttQualityOfServiceLevel qos, bool keepInflight)
    {
        var options = new MqttServerOptionsBuilder().WithMaxPendingMessagesPerClient(1)
            .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage).Build();
        var events = new MqttServerEventContainer();
        var logger = new MqttNetNullLogger();
        using var retained = new MqttRetainedMessagesManager(events, logger);
        using var sessions = new MqttClientSessionsManager(options, retained, events, logger);
        using var session = new MqttSession(new MqttConnectPacket { ClientId = "eviction-recovery" }, new Hashtable(), options, events, retained, sessions);
        MqttPublishPacket inflight = null;
        if (keepInflight)
        {
            inflight = new MqttPublishPacket { Topic = "inflight", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce };
            session.EnqueueDataPacket(new MqttPacketBusItem(inflight));
            var sent = await session.DequeuePacketAsync(CancellationToken.None);
            sent.Complete(); // Sent but not acknowledged: this packet must remain recoverable.
        }

        var evicted = new MqttPublishPacket { Topic = "evicted", QualityOfServiceLevel = qos };
        var evictedItem = new MqttPacketBusItem(evicted);
        session.EnqueueDataPacket(evictedItem);
        var evictedIdentifier = evicted.PacketIdentifier;
        var replacement = new MqttPublishPacket { Topic = "replacement", QualityOfServiceLevel = qos };
        session.EnqueueDataPacket(new MqttPacketBusItem(replacement));
        Assert.IsInstanceOfType<MqttPendingMessagesOverflowException>(evictedItem.WaitAsync().Exception?.InnerException);
        var replacementItem = await session.DequeuePacketAsync(CancellationToken.None);
        Assert.AreSame(replacement, replacementItem.Packet);
        replacementItem.Complete();
        if (qos != MqttQualityOfServiceLevel.AtMostOnce)
        {
            Assert.AreSame(replacement, session.AcknowledgePublishPacket(replacement.PacketIdentifier));
        }

        var evictedStillTracked = qos != MqttQualityOfServiceLevel.AtMostOnce && session.PeekAcknowledgePublishPacket(evictedIdentifier) != null;
        if (keepInflight) Assert.AreSame(inflight, session.PeekAcknowledgePublishPacket(inflight.PacketIdentifier));
        // Avoid a second overflow during recovery so this test isolates eviction bookkeeping.
        options.MaxPendingMessagesPerClient = 4;
        session.Recover();
        var recovered = new List<MqttPublishPacket>();
        while (session.PendingDataPacketsCount > 0)
        {
            recovered.Add((MqttPublishPacket)(await session.DequeuePacketAsync(CancellationToken.None)).Packet);
        }

        TestContext.WriteLine($"Evicted tracked before recovery: {evictedStillTracked}; recovered: {string.Join(",", recovered.Select(p => p.Topic))}");
        Assert.HasCount(keepInflight ? 1 : 0, recovered);
        Assert.IsFalse(evictedStillTracked);
        if (keepInflight)
        {
            Assert.AreSame(inflight, recovered.Single());
            Assert.AreSame(inflight, session.AcknowledgePublishPacket(inflight.PacketIdentifier));
        }

        session.Recover();
        Assert.AreEqual(0, (int)session.PendingDataPacketsCount, "Acknowledged replacement or evicted packet reappeared on the next recovery.");
    }
}
