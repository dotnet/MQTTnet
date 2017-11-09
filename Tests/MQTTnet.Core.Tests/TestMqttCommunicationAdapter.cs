using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Tests
{
    public class TestMqttCommunicationAdapter : IMqttCommunicationAdapter
    {
        private readonly BlockingCollection<MqttBasePacket> _incomingPackets = new BlockingCollection<MqttBasePacket>();

        public TestMqttCommunicationAdapter Partner { get; set; }

        public IMqttPacketSerializer PacketSerializer { get; } = new MqttPacketSerializer();

        public Task ConnectAsync(TimeSpan timeout)
        {
            return Task.FromResult(0);
        }

        public Task DisconnectAsync(TimeSpan timeout)
        {
            return Task.FromResult(0);
        }

        public Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<MqttBasePacket> packets)
        {
            ThrowIfPartnerIsNull();

            foreach (var packet in packets)
            {
                Partner.EnqueuePacketInternal(packet);
            }

            return Task.FromResult(0);
        }

        public Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfPartnerIsNull();

            return Task.Run(() => _incomingPackets.Take(), cancellationToken);
        }

        private void EnqueuePacketInternal(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            _incomingPackets.Add(packet);
        }

        private void ThrowIfPartnerIsNull()
        {
            if (Partner == null)
            {
                throw new InvalidOperationException("Partner is not set.");
            }
        }
    }
}
