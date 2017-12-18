using System;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(MqttPacketHeader header, byte[] body)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public MqttPacketHeader Header { get; }

        public byte[] Body { get; }
    }
}
