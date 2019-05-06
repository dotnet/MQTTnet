using MQTTnet.Formatter;

namespace MQTTnet.Adapter
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(byte fixedHeader, IMqttPacketBodyReader body, int totalLength)
        {
            FixedHeader = fixedHeader;
            Body = body;
            TotalLength = totalLength;
        }

        public byte FixedHeader { get; set; }

        public IMqttPacketBodyReader Body { get; }

        public int TotalLength { get; }
    }
}
