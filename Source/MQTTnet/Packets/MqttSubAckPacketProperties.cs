using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public class MqttSubAckPacketProperties
    {
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
    }
}
