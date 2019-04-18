using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Mockups
{
    public class TestMqttCommunicationAdapter : IMqttChannelAdapter
    {
        private readonly BlockingCollection<MqttBasePacket> _incomingPackets = new BlockingCollection<MqttBasePacket>();

        public TestMqttCommunicationAdapter Partner { get; set; }

        public string Endpoint { get; } = string.Empty;

        public bool IsSecureConnection { get; } = false;

        public MqttPacketFormatterAdapter PacketFormatterAdapter { get; } = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311);

        public long BytesSent { get; }
        public long BytesReceived { get; }

        public Action ReadingPacketStartedCallback { get; set; }
        public Action ReadingPacketCompletedCallback { get; set; }

        public void Dispose()
        {
        }

        public Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfPartnerIsNull();

            Partner.EnqueuePacketInternal(packet);

            return Task.FromResult(0);
        }

        public async ValueTask<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfPartnerIsNull();
            
            return await Task.Run(() =>
            {
                try
                {
                    return _incomingPackets.Take(cancellationToken);
                }
                catch
                {
                    return null;
                }
            }, cancellationToken);
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
