using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties.BaseTypes
{
    public class TwoByteIntegerProperty : IProperty
    {
        public TwoByteIntegerProperty(byte id, ushort value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public ushort Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.Write(Value);
        }
    }
}
