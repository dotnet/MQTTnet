using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public class StringValue : IPropertyValue
    {
        public StringValue(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteWithLengthPrefix(Value);
        }
    }
}
