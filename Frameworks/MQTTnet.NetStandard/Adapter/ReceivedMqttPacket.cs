using System;
using System.IO;
using MQTTnet.Packets;

namespace MQTTnet.Adapter
{
    public sealed class ReceivedMqttPacket
    {
        public ReceivedMqttPacket(MqttPacketHeader header, Memory<byte> body)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Body = body;
        }

        public MqttPacketHeader Header { get; }

        public Memory<byte> Body { get; }
    }
}
