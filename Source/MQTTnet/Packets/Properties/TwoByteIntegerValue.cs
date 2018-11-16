using System;
using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public class TwoByteIntegerValue : IPropertyValue
    {
        public TwoByteIntegerValue(ushort value)
        {
            Value = value;
        }

        public ushort Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            throw new NotImplementedException();
        }
    }
}
