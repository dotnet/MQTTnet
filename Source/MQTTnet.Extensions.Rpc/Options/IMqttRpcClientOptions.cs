using MQTTnet.Extensions.Rpc.Options.TopicGeneration;

namespace MQTTnet.Extensions.Rpc.Options
{
    public interface IMqttRpcClientOptions
    {
        IMqttRpcClientTopicGenerationStrategy TopicGenerationStrategy { get; set; }
    }
}