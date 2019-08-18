using MQTTnet.Extensions.Rpc.Options.TopicGeneration;

namespace MQTTnet.Extensions.Rpc.Options
{
    public class MqttRpcClientOptions : IMqttRpcClientOptions
    {
        public IMqttRpcClientTopicGenerationStrategy TopicGenerationStrategy { get; set; } = new DefaultMqttRpcClientTopicGenerationStrategy();
    }
}
