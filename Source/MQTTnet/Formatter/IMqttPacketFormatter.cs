using System;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public interface IMqttPacketFormatter
    {
        ArraySegment<byte> Encode(MqttBasePacket mqttPacket);

        MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket);

        MqttPublishPacket ConvertApplicationMessageToPublishPacket(MqttApplicationMessage applicationMessage);

        void FreeBuffer();
    }
}