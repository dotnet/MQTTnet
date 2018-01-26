using System;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(MqttPacketHeader header, ArraySegment<byte> body)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Body = body;
        }

        public MqttPacketHeader Header { get; }

        public ArraySegment<byte> Body { get; }
    }
}
