// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Internal;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[TestClass]
public sealed class MqttPacketBus_Tests
{
    [TestMethod]
    public void Alternate_Priorities()
    {
        var bus = new MqttPacketBus();

        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);

        bus.EnqueueItem(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);

        bus.EnqueueItem(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);

        Assert.AreEqual(9, bus.TotalItemsCount);

        Assert.IsInstanceOfType<MqttPublishPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttSubAckPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttPingRespPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttPublishPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttSubAckPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttPingRespPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttPublishPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttSubAckPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);
        Assert.IsInstanceOfType<MqttPingRespPacket>(bus.DequeueItemAsync(CancellationToken.None).Result.Packet);

        Assert.AreEqual(0, bus.TotalItemsCount);
    }

    [TestMethod]
    public void Await_Single_Packet()
    {
        var bus = new MqttPacketBus();

        var delivered = false;

        var item1 = new MqttPacketBusItem(new MqttPublishPacket());
        var item2 = new MqttPacketBusItem(new MqttPublishPacket());

        var item3 = new MqttPacketBusItem(new MqttPublishPacket());
        item3.Completed += (_, _) =>
        {
            delivered = true;
        };

        bus.EnqueueItem(item1, MqttPacketBusPartition.Data);
        bus.EnqueueItem(item2, MqttPacketBusPartition.Data);
        bus.EnqueueItem(item3, MqttPacketBusPartition.Data);

        Assert.IsFalse(delivered);

        bus.DequeueItemAsync(CancellationToken.None).Result.Complete();

        Assert.IsFalse(delivered);

        bus.DequeueItemAsync(CancellationToken.None).Result.Complete();

        Assert.IsFalse(delivered);

        bus.DequeueItemAsync(CancellationToken.None).Result.Complete();

        // The third packet has the event attached.
        Assert.IsTrue(delivered);
    }

    [TestMethod]
    public void Export_Packets_Without_Dequeue()
    {
        var bus = new MqttPacketBus();

        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
        bus.EnqueueItem(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);

        Assert.AreEqual(3, bus.TotalItemsCount);

        var exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Control);
        Assert.HasCount(0, exportedPackets);

        exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Health);
        Assert.HasCount(0, exportedPackets);

        exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Data);
        Assert.HasCount(3, exportedPackets);

        Assert.AreEqual(3, bus.TotalItemsCount);
    }

    [TestMethod]
    public async Task Fill_From_Different_Task()
    {
        const int messageCount = 500;

        var delayRandom = new Random();

        var bus = new MqttPacketBus();

        _ = Task.Run(
            () =>
            {
                for (var i = 0; i < messageCount; i++)
                {
                    bus.EnqueueItem(new MqttPacketBusItem(MqttPingReqPacket.Instance), MqttPacketBusPartition.Health);

                    Thread.Sleep(delayRandom.Next(0, 10));
                }
            });

        for (var i = 0; i < messageCount; i++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await bus.DequeueItemAsync(timeout.Token);
        }

        Assert.AreEqual(0, bus.TotalItemsCount);
    }

    [TestMethod]
    public Task Wait_With_Empty_Bus()
    {
        return Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            var bus = new MqttPacketBus();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await bus.DequeueItemAsync(timeout.Token);
        });

    }
}