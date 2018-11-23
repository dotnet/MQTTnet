using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public class MqttApplicationMessage
    {
        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public bool Retain { get; set; }
    }
}
