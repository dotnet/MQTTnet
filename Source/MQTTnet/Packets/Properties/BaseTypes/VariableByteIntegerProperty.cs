using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties.BaseTypes
{
    public class VariableByteIntegerProperty : IProperty
    {
        public VariableByteIntegerProperty(byte id, uint value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public uint Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            var buffer = MqttPacketWriter.EncodeVariableByteInteger(Value);
            writer.Write(buffer);
        }
    }
}
