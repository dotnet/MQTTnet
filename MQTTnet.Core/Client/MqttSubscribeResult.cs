using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Client
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
