using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttCommunicationAdapter
    {
        IMqttPacketSerializer PacketSerializer { get; }

        Task ConnectAsync(TimeSpan timeout, MqttClientOptions options);

        Task DisconnectAsync(TimeSpan timeout);

        Task SendPacketsAsync(TimeSpan timeout, IEnumerable<MqttBasePacket> packets);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout);
    }
}
