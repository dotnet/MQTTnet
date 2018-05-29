using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttSubscribeResult
    {
        public MqttSubscribeResult(TopicFilter topicFilter, MqttSubscribeReturnCode returnCode)
        {
            TopicFilter = topicFilter;
            ReturnCode = returnCode;
        }

        public TopicFilter TopicFilter { get; }

        public MqttSubscribeReturnCode ReturnCode { get; }
    }
}
