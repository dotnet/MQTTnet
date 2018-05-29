using System;
using System.IO;

namespace MQTTnet.Adapter
{
    public sealed class ReceivedMqttPacket : IDisposable
    {
        public ReceivedMqttPacket(byte fixedHeader, MemoryStream body)
        {
            FixedHeader = fixedHeader;
            Body = body;
        }

        public byte FixedHeader { get; }

        public MemoryStream Body { get; }

        public void Dispose()
        {
            Body?.Dispose();
        }
    }
}
