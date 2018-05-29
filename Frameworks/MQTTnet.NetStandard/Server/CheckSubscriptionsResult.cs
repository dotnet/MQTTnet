using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class CheckSubscriptionsResult
    {
        public bool IsSubscribed { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
