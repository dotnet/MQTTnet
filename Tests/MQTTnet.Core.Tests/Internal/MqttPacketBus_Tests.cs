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
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Wait_With_Empty_Bus()
        {
            var bus = new MqttPacketBus();

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            {
                await bus.DequeueAsync(timeout.Token);
            }
        }
        
        [TestMethod]
        public void Alternate_Priorities()
        {
            var bus = new MqttPacketBus();
            
            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            
            bus.Enqueue(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);
            bus.Enqueue(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);
            bus.Enqueue(new MqttPacketBusItem(new MqttSubAckPacket()), MqttPacketBusPartition.Control);
            
            bus.Enqueue(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);
            bus.Enqueue(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);
            bus.Enqueue(new MqttPacketBusItem(new MqttPingRespPacket()), MqttPacketBusPartition.Health);

            Assert.AreEqual(9, bus.PacketsCount);
            
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPublishPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttSubAckPacket));
            Assert.IsInstanceOfType(bus.DequeueAsync(CancellationToken.None).Result.Packet, typeof(MqttPingRespPacket));
            
            Assert.AreEqual(0, bus.PacketsCount);
        }
        
        [TestMethod]
        public void Export_Packets_Without_Dequeue()
        {
            var bus = new MqttPacketBus();

            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPacketBusItem(new MqttPublishPacket()), MqttPacketBusPartition.Data);
            
            Assert.AreEqual(3, bus.PacketsCount);

            var exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Control);
            Assert.AreEqual(0, exportedPackets.Count);
            
            exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Health);
            Assert.AreEqual(0, exportedPackets.Count);
            
            exportedPackets = bus.ExportPackets(MqttPacketBusPartition.Data);
            Assert.AreEqual(3, exportedPackets.Count);
            
            Assert.AreEqual(3, bus.PacketsCount);
        }
        
        [TestMethod]
        public void Await_Single_Packet()
        {
            var bus = new MqttPacketBus();

            var delivered = false;
            
            bus.Enqueue(new MqttPublishPacket(), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPublishPacket(), MqttPacketBusPartition.Data);
            bus.Enqueue(new MqttPublishPacket(), MqttPacketBusPartition.Data).Delivered += (_, __) =>
            {
                delivered = true;
            };
            
            Assert.IsFalse(delivered);

            bus.DequeueAsync(CancellationToken.None).Result.MarkAsDelivered();
            
            Assert.IsFalse(delivered);
            
            bus.DequeueAsync(CancellationToken.None).Result.MarkAsDelivered();
            
            Assert.IsFalse(delivered);
            
            bus.DequeueAsync(CancellationToken.None).Result.MarkAsDelivered();
            
            // The third packet has the event attached.
            Assert.IsTrue(delivered);
        }
    }
}