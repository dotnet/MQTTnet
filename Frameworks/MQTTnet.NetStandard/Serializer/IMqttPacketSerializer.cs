using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        byte[] Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket);
    }
}