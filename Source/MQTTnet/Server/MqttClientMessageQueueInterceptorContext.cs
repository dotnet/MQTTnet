using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientMessageQueueInterceptorContext
    {
        public MqttClientMessageQueueInterceptorContext(string senderClientId, string receiverClientId, MqttApplicationMessage applicationMessage, MqttQualityOfServiceLevel subscriptionQualityOfServiceLevel)
        {
            SenderClientId = senderClientId;
            ReceiverClientId = receiverClientId;
            ApplicationMessage = applicationMessage;
            SubscriptionQualityOfServiceLevel = subscriptionQualityOfServiceLevel;
        }

        public string SenderClientId { get; }

        public string ReceiverClientId { get; }

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
