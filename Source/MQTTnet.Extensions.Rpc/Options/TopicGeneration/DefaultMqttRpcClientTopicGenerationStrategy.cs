using System;

namespace MQTTnet.Extensions.Rpc.Options.TopicGeneration
{
    public sealed class DefaultMqttRpcClientTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
    {
        public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
        {
            if (context.MethodName.Contains("/") || context.MethodName.Contains("+") || context.MethodName.Contains("#"))
            {
                throw new ArgumentException("The method name cannot contain /, + or #.");
            }
            
            var requestTopic = $"MQTTnet.RPC/{Guid.NewGuid():N}/{context.MethodName}";
            var responseTopic = requestTopic + "/response";

            return new MqttRpcTopicPair
            {
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic
            };
        }
    }
}
