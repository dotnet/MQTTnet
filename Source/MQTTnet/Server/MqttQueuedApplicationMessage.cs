using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttQueuedApplicationMessage
    {
        public MqttApplicationMessage ApplicationMessage { get; set; }

        public string SenderClientId { get; set; }

        public bool IsRetainedMessage { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool IsDuplicate { get; set; }
    }
}