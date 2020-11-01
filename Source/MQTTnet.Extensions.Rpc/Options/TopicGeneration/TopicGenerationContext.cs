using MQTTnet.Client;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.Rpc.Options.TopicGeneration
{
    public sealed class TopicGenerationContext
    {
        public string MethodName { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public IMqttClient MqttClient { get; set; }

        public IMqttRpcClientOptions Options { get; set; }
    }
}
