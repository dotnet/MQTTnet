using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public interface IMqttPacketFormatter
    {
        MqttPacketBuffer Encode(MqttBasePacket mqttPacket);

        MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket);

        void FreeBuffer();
    }
}