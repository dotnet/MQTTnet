using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Tests
{
    public class TestMqttCommunicationAdapter : IMqttCommunicationAdapter
    {
        private readonly BlockingCollection<MqttBasePacket> _incomingPackets = new BlockingCollection<MqttBasePacket>();

        public TestMqttCommunicationAdapter Partner { get; set; }

        public IMqttPacketSerializer PacketSerializer { get; } = new MqttPacketSerializer();

        public async Task ConnectAsync(MqttClientOptions options, TimeSpan timeout)
        {
            await Task.FromResult(0);
        }

        public async Task DisconnectAsync()
        {
            await Task.FromResult(0);
        }

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout)
        {
            ThrowIfPartnerIsNull();

            Partner.SendPacketInternal(packet);
            await Task.FromResult(0);
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            ThrowIfPartnerIsNull();

            return await Task.Run(() => _incomingPackets.Take());
        }

        private void SendPacketInternal(MqttBasePacket packet)
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
