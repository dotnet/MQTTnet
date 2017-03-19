using System;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core
{
    public class MqttApplicationMessage
    {
        public MqttApplicationMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            Topic = topic;
            Payload = payload;
            QualityOfServiceLevel = qualityOfServiceLevel;
            Retain = retain;
        }

        public string Topic { get; }

        public byte[] Payload { get; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }

        public bool Retain { get; }
    }
}
