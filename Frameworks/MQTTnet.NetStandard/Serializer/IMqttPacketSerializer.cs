using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public interface IMqttPacketSerializer
    {
        MqttProtocolVersion ProtocolVersion { get; set; }

        IEnumerable<ArraySegment<byte>> Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket);
    }
}