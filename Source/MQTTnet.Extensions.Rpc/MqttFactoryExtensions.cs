using System;
using MQTTnet.Client;
using MQTTnet.Extensions.Rpc.Options;
using MQTTnet.Extensions.Rpc.Options.TopicGeneration;

namespace MQTTnet.Extensions.Rpc
{
    public static class MqttFactoryExtensions
    {
        public static IMqttRpcClient CreateMqttRpcClient(this MqttFactory factory, IMqttClient mqttClient)
        {
            return factory.CreateMqttRpcClient(mqttClient, new MqttRpcClientOptions
            {
                TopicGenerationStrategy = new DefaultMqttRpcClientTopicGenerationStrategy()
            });
        }

        public static IMqttRpcClient CreateMqttRpcClient(this MqttFactory factory, IMqttClient mqttClient, IMqttRpcClientOptions rpcClientOptions)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            if (rpcClientOptions == null)
            {
                throw new ArgumentNullException(nameof(rpcClientOptions));
            }

            return new MqttRpcClient(mqttClient, rpcClientOptions);
        }
    }
}