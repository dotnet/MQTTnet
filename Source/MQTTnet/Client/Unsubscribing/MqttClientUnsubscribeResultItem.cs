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

        /// <summary>
        /// Gets or sets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public string TopicFilter { get; }

        /// <summary>
        /// Gets or sets the result code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientUnsubscribeResultCode ReasonCode { get; }
    }
}