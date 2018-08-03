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

        MqttPacketFormatterAdapter PacketFormatterAdapter { get; }
        
        event EventHandler ReadingPacketStarted;

        event EventHandler<MqttBasePacket> ReadingPacketCompleted;

        Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task DisconnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

        Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken);
        
        Task ReceivePacketAsync(CancellationToken cancellationToken);
    }
}
