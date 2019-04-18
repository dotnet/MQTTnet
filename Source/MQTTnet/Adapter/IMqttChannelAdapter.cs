using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public interface IMqttChannelAdapter : IDisposable
    {
        string Endpoint { get; }

        bool IsSecureConnection { get; }

        MqttPacketFormatterAdapter PacketFormatterAdapter { get; }

        long BytesSent { get; }

        long BytesReceived { get; }

        Action ReadingPacketStartedCallback { get; set; }

        Action ReadingPacketCompletedCallback { get; set; }

        Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout, CancellationToken cancellationToken);

        ValueTask<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
