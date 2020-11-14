using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubscribePacketProperties
    {
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
