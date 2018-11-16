using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public class ByteValue : IPropertyValue
    {
        public ByteValue(byte value)
        {
            Value = value;
        }

        public byte Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            
            writer.Write(Value);
        }
    }
}
