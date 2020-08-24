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

        public MqttQualityOfServiceLevel SubscriptionQualityOfServiceLevel { get; set; }
    }
}
