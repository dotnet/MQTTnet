using System;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttQueuedApplicationMessage
    {
        public MqttApplicationMessage ApplicationMessage { get; set; }

        public string SenderClientId { get; set; }

        public bool IsRetainedMessage { get; set; }

        public MqttQualityOfServiceLevel SubscriptionQualityOfServiceLevel { get; set; }

        [Obsolete("Use 'SubscriptionQualityOfServiceLevel' instead.")]
        public MqttQualityOfServiceLevel QualityOfServiceLevel
        {
            get => SubscriptionQualityOfServiceLevel;
            set => SubscriptionQualityOfServiceLevel = value;
        }

        public bool IsDuplicate { get; set; }
    }
}