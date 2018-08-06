using System;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public interface IMqttPacketSerializer
    {
        ArraySegment<byte> Serialize(MqttBasePacket mqttPacket);

        MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket);

        void FreeBuffer();
    }
}