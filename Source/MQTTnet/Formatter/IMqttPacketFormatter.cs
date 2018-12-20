using System;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public interface IMqttPacketFormatter
    {
        IMqttDataConverter DataConverter { get; }

        ArraySegment<byte> Encode(MqttBasePacket mqttPacket);

        MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket);

        void FreeBuffer();
    }
}