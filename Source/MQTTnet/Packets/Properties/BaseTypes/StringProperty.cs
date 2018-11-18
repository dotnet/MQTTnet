using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
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

            writer.WriteWithLengthPrefix(Value);
        }
    }
}
