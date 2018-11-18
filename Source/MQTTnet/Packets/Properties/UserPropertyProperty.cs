using System;
using MQTTnet.Formatter;

namespace MQTTnet.Packets.Properties
{
    public class UserPropertyProperty : IProperty
    {
        public UserPropertyProperty(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public byte Id { get; } = (byte)MqttMessagePropertyID.UserProperty;

        public string Name { get; }

        public string Value { get; }

        public void WriteTo(MqttPacketWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            
            writer.Write(Id);
            writer.WriteWithLengthPrefix(Name);
            writer.WriteWithLengthPrefix(Value);
        }
    }
}
