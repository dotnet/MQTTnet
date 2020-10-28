using System;

namespace MQTTnet.Packets
{
    public sealed class MqttUserProperty
    {
        public MqttUserProperty(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Value { get; }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Value.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return Equals(other as MqttUserProperty);
        }

        public bool Equals(MqttUserProperty other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                string.Equals(Value, other.Value, StringComparison.Ordinal);
        }
    }
}
