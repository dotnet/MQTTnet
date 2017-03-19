using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Serializer
{
    public interface IMqttPacketSerializer
    {
        Task SerializeAsync(MqttBasePacket mqttPacket, IMqttTransportChannel destination);

        Task<MqttBasePacket> DeserializeAsync(IMqttTransportChannel source);
    }
}