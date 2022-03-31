// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public static class MqttClientExtensions
    {
        public static Task DisconnectAsync(this IMqttClient client, MqttClientDisconnectReason reason = MqttClientDisconnectReason.NormalDisconnection, string reasonString = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return client.DisconnectAsync(
                new MqttClientDisconnectOptions
                {
                    Reason = reason,
                    ReasonString = reasonString
                },
                CancellationToken.None);
        }

        public static Task<MqttClientPublishResult> PublishBinaryAsync(
            this IMqttClient mqttClient,
            string topic,
            IEnumerable<byte> payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
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

            return mqttClient.PublishAsync(applicationMessage, cancellationToken);
        }

        public static Task<MqttClientPublishResult> PublishStringAsync(
            this IMqttClient mqttClient,
            string topic,
            string payload = null,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
        {
            var payloadBuffer = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            return mqttClient.PublishBinaryAsync(topic, payloadBuffer, qualityOfServiceLevel, retain, cancellationToken);
        }

        public static Task SendExtendedAuthenticationExchangeDataAsync(this IMqttClient mqttClient, MqttExtendedAuthenticationExchangeData data)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            return mqttClient.SendExtendedAuthenticationExchangeDataAsync(data, CancellationToken.None);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient mqttClient, MqttTopicFilter topicFilter, CancellationToken cancellationToken = default)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            if (topicFilter == null)
            {
                throw new ArgumentNullException(nameof(topicFilter));
            }

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter)
                .Build();

            return mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(
            this IMqttClient mqttClient,
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            CancellationToken cancellationToken = default)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, qualityOfServiceLevel)
                .Build();

            return mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
        }

        public static Task<MqttClientUnsubscribeResult> UnsubscribeAsync(this IMqttClient mqttClient, string topic, CancellationToken cancellationToken = default)
        {
            if (mqttClient == null)
            {
                throw new ArgumentNullException(nameof(mqttClient));
            }

            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic)
                .Build();

            return mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken);
        }
    }
}