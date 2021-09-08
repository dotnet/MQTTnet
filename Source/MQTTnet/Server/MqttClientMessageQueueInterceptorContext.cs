using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientMessageQueueInterceptorContext
    {
        public string SenderClientId { get; internal set; }

        public string ReceiverClientId { get; internal set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        public bool AcceptEnqueue { get; set; } = true;

        /// <summary>
        /// Gets or sets the supscription quality of service level.
        /// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message.
        /// There are 3 QoS levels in MQTT:
        /// - At most once  (0): Message gets delivered no time, once or multiple times.
        /// - At least once (1): Message gets delivered at least once (one time or more often).
        /// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        public MqttQualityOfServiceLevel SubscriptionQualityOfServiceLevel { get; set; }
    }
}
