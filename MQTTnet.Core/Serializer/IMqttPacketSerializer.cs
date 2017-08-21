using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        Task SerializeAsync(MqttBasePacket mqttPacket, IMqttCommunicationChannel destination);

        Task<MqttBasePacket> DeserializeAsync(IMqttCommunicationChannel source);
    }
}