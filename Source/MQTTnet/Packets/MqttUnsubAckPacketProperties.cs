using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubAckPacketProperties
    {
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
