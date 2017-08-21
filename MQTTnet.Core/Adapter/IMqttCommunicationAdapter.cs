using System;
using System.Threading.Tasks;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttCommunicationAdapter
    {
        Task ConnectAsync(MqttClientOptions options, TimeSpan timeout);

        Task DisconnectAsync();

        Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout);

        Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout);

        IMqttPacketSerializer PacketSerializer { get; }
    }
}
