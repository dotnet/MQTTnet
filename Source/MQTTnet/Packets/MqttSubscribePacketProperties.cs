using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public class MqttSubscribePacketProperties
    {
        public uint? SubscriptionIdentifier { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
