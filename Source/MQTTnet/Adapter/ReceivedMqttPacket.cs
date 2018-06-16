using MQTTnet.Serializer;
using System;

namespace MQTTnet.Adapter
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(byte fixedHeader, Memory<byte> body)
        {
            FixedHeader = fixedHeader;
            Body = body;
        }

        public byte FixedHeader { get; }

        public Memory<byte> Body { get; }
    }
}
