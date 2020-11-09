using System;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Formatter.V5
{
    public sealed class MqttV500PacketFormatter : IMqttPacketFormatter
    {
        readonly MqttV500PacketDecoder _decoder = new MqttV500PacketDecoder();
        readonly MqttV500PacketEncoder _encoder;

        public MqttV500PacketFormatter(IMqttPacketWriter writer)
        {
            _encoder = new MqttV500PacketEncoder(writer);
        }

        public IMqttDataConverter DataConverter { get; } = new MqttV500DataConverter();
        
        public ArraySegment<byte> Encode(MqttBasePacket mqttPacket)
        {
            if (mqttPacket == null) throw new ArgumentNullException(nameof(mqttPacket));

            return _encoder.Encode(mqttPacket);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            return _decoder.Decode(receivedMqttPacket);
        }
        
        public void FreeBuffer()
        {
            _encoder.FreeBuffer();
        }
    }
}
