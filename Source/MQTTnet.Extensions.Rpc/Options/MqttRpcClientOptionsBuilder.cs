using MQTTnet.Extensions.Rpc.Options.TopicGeneration;
using System;

namespace MQTTnet.Extensions.Rpc.Options
{
    public sealed class MqttRpcClientOptionsBuilder
    {
        IMqttRpcClientTopicGenerationStrategy _topicGenerationStrategy = new DefaultMqttRpcClientTopicGenerationStrategy();

        public MqttRpcClientOptionsBuilder WithTopicGenerationStrategy(IMqttRpcClientTopicGenerationStrategy value)
        {
            _topicGenerationStrategy = value ?? throw new ArgumentNullException(nameof(value));

            return this;
        }

        public IMqttRpcClientOptions Build()
        {
            return new MqttRpcClientOptions
            {
                TopicGenerationStrategy = _topicGenerationStrategy
            };
        }
    }
}
