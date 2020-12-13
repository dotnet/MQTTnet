using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public struct CheckSubscriptionsResult
    {
        public static CheckSubscriptionsResult NotSubscribed = new CheckSubscriptionsResult();

        public bool IsSubscribed { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
