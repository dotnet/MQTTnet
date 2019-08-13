namespace MQTTnet.Extensions.Rpc.Options.TopicGeneration
{
    public interface IMqttRpcClientTopicGenerationStrategy
    {
        MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context);
    }
}
