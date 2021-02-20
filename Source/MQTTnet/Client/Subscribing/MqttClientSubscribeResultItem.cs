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

        /// <summary>
        /// Gets or sets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; }

        /// <summary>
        /// Gets or sets the result code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientSubscribeResultCode ResultCode { get; }
    }
}
