using System;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeResultItem
    {
        public MqttClientSubscribeResultItem(MqttTopicFilter topicFilter, MqttClientSubscribeResultCode resultCode)
        {
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            ResultCode = resultCode;
        }

        public MqttTopicFilter TopicFilter { get; }

        public MqttClientSubscribeResultCode ResultCode { get; }
    }
}
