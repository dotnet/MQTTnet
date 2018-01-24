using System;
using System.Collections.Generic;
using System.IO;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        ICollection<ArraySegment<byte>> Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(MqttPacketHeader header, MemoryStream body);
    }
}