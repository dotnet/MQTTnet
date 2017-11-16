using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public class CheckSubscriptionsResult
    {
        public bool IsSubscribed { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
    }
}
