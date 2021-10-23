namespace MQTTnet.Client.Subscribing
{
    public sealed class MqttClientSubscribeResultItem
    {
        /// <summary>
        /// Gets or sets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; internal set; }

        /// <summary>
        /// Gets or sets the result code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientSubscribeResultCode ResultCode { get; internal set; }
    }
}
