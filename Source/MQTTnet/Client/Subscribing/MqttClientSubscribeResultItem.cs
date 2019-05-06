using System;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeResultItem
    {
        public MqttClientSubscribeResultItem(TopicFilter topicFilter, MqttClientSubscribeResultCode resultCode)
        {
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            ResultCode = resultCode;
        }

        public TopicFilter TopicFilter { get; }

        public MqttClientSubscribeResultCode ResultCode { get; }
    }
}
