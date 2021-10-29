namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectOptions
    {
        /// <summary>
        /// Gets or sets the reason code.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientDisconnectReason Reason { get; set; } = MqttClientDisconnectReason.NormalDisconnection;

        /// <summary>
        /// Gets or sets the reason string.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; set; }
    }
}
