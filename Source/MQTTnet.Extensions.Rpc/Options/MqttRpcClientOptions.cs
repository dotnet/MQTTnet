using MQTTnet.Extensions.Rpc.Options.TopicGeneration;

namespace MQTTnet.Extensions.Rpc.Options
{
    public sealed class MqttRpcClientOptions : IMqttRpcClientOptions
    {
        public IMqttRpcClientTopicGenerationStrategy TopicGenerationStrategy { get; set; } = new DefaultMqttRpcClientTopicGenerationStrategy();
    }
}
