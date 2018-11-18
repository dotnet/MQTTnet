using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public class ByteProperty : IProperty
    {
        public ByteProperty(byte id, byte value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public byte Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            
            writer.Write(Value);
        }
    }
}
