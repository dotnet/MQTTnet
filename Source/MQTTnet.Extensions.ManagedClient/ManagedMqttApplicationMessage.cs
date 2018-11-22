using System;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttApplicationMessage : IEquatable<ManagedMqttApplicationMessage>
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public MqttApplicationMessage ApplicationMessage { get; set; }
       
        public bool Equals(ManagedMqttApplicationMessage other)
        {
            return Id.Equals(other.Id);
        }
    }
}
