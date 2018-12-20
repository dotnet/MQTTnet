using System;

namespace MQTTnet.Client.Unsubscribing
{
    public class MqttClientUnsubscribeResultItem
    {
        public MqttClientUnsubscribeResultItem(string topicFilter, MqttClientUnsubscribeResultCode reasonCode)
        {
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            ReasonCode = reasonCode;
        }

        public string TopicFilter { get; }

        public MqttClientUnsubscribeResultCode ReasonCode { get; }
    }
}