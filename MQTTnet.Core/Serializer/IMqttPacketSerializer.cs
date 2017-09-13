using System.IO;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        byte[] Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(MqttPacketHeader header, MemoryStream stream);
    }
}