// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static Task<MqttClientPublishResult> PublishAsync(
            this ManagedMqttClient managedMqttClient,
            string topic,
            string payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
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

            return managedMqttClient.PublishAsync(applicationMessage, cancellationToken);
        }

        public static Task SubscribeAsync(
            this ManagedMqttClient managedMqttClient,
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

            return managedMqttClient.SubscribeAsync(
                new List<MqttTopicFilter>
                {
                    new MqttTopicFilterBuilder().WithTopic(topic)
                        .WithQualityOfServiceLevel(qualityOfServiceLevel)
                        .Build()
                });
        }

        public static Task UnsubscribeAsync(this ManagedMqttClient managedMqttClient, string topic)
        {
            if (managedMqttClient == null)
            {
                throw new ArgumentNullException(nameof(managedMqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return managedMqttClient.UnsubscribeAsync(new List<string> { topic });
        }
    }
}