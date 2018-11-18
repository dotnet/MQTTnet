using System;
using MQTTnet.Formatter;

namespace MQTTnet.Packets.Properties.BaseTypes
{
    public class FourByteIntegerValue : IProperty
    {
        public FourByteIntegerValue(byte id, uint value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public uint Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            // TODO: Check if order must be reversed like for ushort.
            writer.Write(Id);
            var bytes = BitConverter.GetBytes(Value);
            writer.Write(bytes, 0, bytes.Length);
        }
    }
}
