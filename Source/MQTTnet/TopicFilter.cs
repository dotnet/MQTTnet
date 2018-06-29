using MQTTnet.Protocol;

namespace MQTTnet
{
    public class TopicFilter
    {
        public TopicFilter(string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            Topic = topic;
            QualityOfServiceLevel = qualityOfServiceLevel;
        }

        public string Topic { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public override string ToString()
        {
            return Topic + "@" + QualityOfServiceLevel;
        }
    }
}