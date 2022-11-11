// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Internal
{
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

            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueItemAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));

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
            item3.Completed += (_, __) =>
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
            Assert.AreEqual(0, exportedPackets.Count);

            exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Health);
            Assert.AreEqual(0, exportedPackets.Count);

            exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Data);
            Assert.AreEqual(3, exportedPackets.Count);

            Assert.AreEqual(3, bus.TotalItemsCount);
        }

        [TestMethod]
        public async Task Fill_From_Different_Task()
        {
            const int MessageCount = 500;

            var delayRandom = new Random();

            var bus = new MqttPacketBus();

            _ = Task.Run(
                () =>
                {
                    for (var i = 0; i < MessageCount; i++)
                    {
                        bus.EnqueueItem(new MqttPacketBusItem(MqttPingReqPacket.Instance), MqttPacketBusPartition.Health);

                        Thread.Sleep(delayRandom.Next(0, 10));
                    }
                });

            for (var i = 0; i < MessageCount; i++)
            {
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    await bus.DequeueItemAsync(timeout.Token);
                }
            }

            Assert.AreEqual(0, bus.TotalItemsCount);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Wait_With_Empty_Bus()
        {
            var bus = new MqttPacketBus();

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            {
                await bus.DequeueItemAsync(timeout.Token);
            }
        }
    }
}