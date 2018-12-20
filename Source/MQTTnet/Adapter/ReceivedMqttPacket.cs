using MQTTnet.Formatter;

namespace MQTTnet.Adapter
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(byte fixedHeader, MqttPacketBodyReader body, int totalLength)
        {
            FixedHeader = fixedHeader;
            Body = body;
            TotalLength = totalLength;
        }

        public byte FixedHeader { get; }

        public MqttPacketBodyReader Body { get; }

        public int TotalLength { get; }
    }
}
