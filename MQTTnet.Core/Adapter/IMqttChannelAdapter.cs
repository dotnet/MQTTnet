using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttChannelAdapter
    {
        IMqttPacketSerializer PacketSerializer { get; }

        Task ConnectAsync(TimeSpan timeout);

        Task DisconnectAsync(TimeSpan timeout);

        Task SendPacketsAsync(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<MqttBasePacket> packets);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
