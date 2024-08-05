// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Extensions.Rpc
{
    public static class MqttFactoryExtensions
    {
        public static IMqttRpcClient CreateMqttRpcClient(this MqttClientFactory clientFactory, IMqttClient mqttClient)
        {
            return clientFactory.CreateMqttRpcClient(
                mqttClient,
                new MqttRpcClientOptions
                {
                    TopicGenerationStrategy = new DefaultMqttRpcClientTopicGenerationStrategy()
                });
        }

        public static IMqttRpcClient CreateMqttRpcClient(this MqttClientFactory _, IMqttClient mqttClient, MqttRpcClientOptions rpcClientOptions)
        {
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