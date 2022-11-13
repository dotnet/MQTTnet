// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static Task EnqueueAsync(
            this IManagedMqttClient managedMqttClient,
            string topic,
            string payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var applicationMessage = new MqttApplicationMessageBuilder().WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .Build();

            return managedMqttClient.EnqueueAsync(applicationMessage);
        }

        public static Task StopAsync(this IManagedMqttClient managedMqttClient, bool isClean = true, CancellationToken cancellationToken = default)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            return managedMqttClient.StopAsync(new MqttClientDisconnectOptions
            {
                IsClean = isClean
            }, cancellationToken);
        }
        
        public static Task SubscribeAsync(this IManagedMqttClient managedMqttClient, MqttTopicFilter topicFilter)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            if (topicFilter == null)
            {
                throw new ArgumentNullException(nameof(topicFilter));
            }

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter).Build();
            return managedMqttClient.SubscribeAsync(subscribeOptions);
        }

        public static Task SubscribeAsync(
            this IManagedMqttClient managedMqttClient,
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, qualityOfServiceLevel).Build();
            return managedMqttClient.SubscribeAsync(subscribeOptions);
        }

        public static Task UnsubscribeAsync(this IManagedMqttClient managedMqttClient, string topic)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic).Build();
            return managedMqttClient.UnsubscribeAsync(unsubscribeOptions);
        }
    }
}