using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public class TopicFilter
    {
        public TopicFilter(string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            Topic = topic;
            QualityOfServiceLevel = qualityOfServiceLevel;
        }

        public string Topic { get; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }
    }
}