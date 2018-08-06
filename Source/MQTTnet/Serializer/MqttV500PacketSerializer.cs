using System;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Serializer
{
    public class MqttV500PacketSerializer : IMqttPacketSerializer
    {
        public ArraySegment<byte> Serialize(MqttBasePacket mqttPacket)
        {
            throw new NotImplementedException();
        }

        public MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket)
        {
            throw new NotImplementedException();
        }

        public void FreeBuffer()
        {
            throw new NotImplementedException();
        }
    }
}
