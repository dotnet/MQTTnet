using System;
using MQTTnet.Protocol;

namespace MQTTnet
{
    public class MqttApplicationMessage
    {
        public MqttApplicationMessage()
        {
        }

        [Obsolete("Use object initializer or _MqttApplicationMessageBuilder_ instead.")]
        public MqttApplicationMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            QualityOfServiceLevel = qualityOfServiceLevel;
            Retain = retain;
        }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Retain { get; set; }
    }
}
