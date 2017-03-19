using System;
using System.Threading.Tasks;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Adapter
{
    public interface IMqttAdapter
    {
        Task ConnectAsync(MqttClientOptions options, TimeSpan timeout);

        Task DisconnectAsync();

        Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout);

        Task<MqttBasePacket> ReceivePacket();
    }
}
