using MQTTnet.Extensions.Rpc.Options.TopicGeneration;
using System;

namespace MQTTnet.Extensions.Rpc.Options
{
    public class DefaultMqttRpcClientTopicGenerationStrategy : IMqttRpcClientTopicGenerationStrategy
    {
        public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
        {
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
