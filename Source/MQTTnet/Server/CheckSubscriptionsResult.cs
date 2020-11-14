using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public struct CheckSubscriptionsResult
    {
        public bool IsSubscribed { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
