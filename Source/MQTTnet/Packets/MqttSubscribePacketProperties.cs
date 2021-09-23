using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttSubscribePacketProperties
    {
        /// <summary>
        /// It is a Protocol Error if the Subscription Identifier has a value of 0.
        /// </summary>
        public uint SubscriptionIdentifier { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; } = new List<MqttUserProperty>();
    }
}
