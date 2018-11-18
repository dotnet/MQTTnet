using System;
using MQTTnet.Formatter;

namespace MQTTnet.Packets.Properties.BaseTypes
{
    public class StringProperty : IProperty
    {
        public StringProperty(byte id, string value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public string Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.Write(Id);
            writer.WriteWithLengthPrefix(Value);
        }
    }
}
