using System;
using System.IO;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        byte[] Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(MqttPacketHeader header, ReadOnlySpan<byte> body);
    }
}