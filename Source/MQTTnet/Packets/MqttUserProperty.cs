using System;

namespace MQTTnet.Packets
{
    public class MqttUserProperty
    {
        public MqttUserProperty(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Value { get; }
    }
}
