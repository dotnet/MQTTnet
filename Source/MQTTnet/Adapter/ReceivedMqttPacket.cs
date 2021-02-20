using System;
using MQTTnet.Formatter;

namespace MQTTnet.Adapter
{
    public sealed class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(byte fixedHeader, IMqttPacketBodyReader bodyReader, int totalLength)
        {
            FixedHeader = fixedHeader;
            BodyReader = bodyReader ?? throw new ArgumentNullException(nameof(bodyReader));
            TotalLength = totalLength;
        }

        public byte FixedHeader { get; }

        public IMqttPacketBodyReader BodyReader { get; }

        public int TotalLength { get; }
    }
}
