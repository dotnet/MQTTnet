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
        public static Task DisconnectAsync(
            this IMqttClient client,
            MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
            string reasonString = null,
            uint sessionExpiryInterval = 0,
            List<MqttUserProperty> userProperties = null,
            CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            var disconnectOptions = new MqttClientDisconnectOptions
            {
                Reason = reason,
                ReasonString = reasonString,
                SessionExpiryInterval = sessionExpiryInterval,
                UserProperties = userProperties
            };

            return client.DisconnectAsync(disconnectOptions, cancellationToken);
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

        public static Task ReconnectAsync(this IMqttClient client, CancellationToken cancellationToken = default)
        {
            if (client.Options == null)
            {
                throw new InvalidOperationException(
                    "The MQTT client was not connected before. A reconnect is only permitted when the client was already connected or at least tried to.");
            }

            return client.ConnectAsync(client.Options, cancellationToken);
        }

        public static Task SendExtendedAuthenticationExchangeDataAsync(this IMqttClient client, MqttExtendedAuthenticationExchangeData data)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return client.SendExtendedAuthenticationExchangeDataAsync(data, CancellationToken.None);
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

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topicFilter).Build();

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

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, qualityOfServiceLevel).Build();

            return mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
        }

        public static async Task<bool> TryDisconnectAsync(
            this IMqttClient client,
            MqttClientDisconnectOptionsReason reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
            string reasonString = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            try
            {
                await client.DisconnectAsync(reason, reasonString).ConfigureAwait(false);
                return true;
            }
            catch
            {
                // Ignore all errors.
            }

            return false;
        }

        public static async Task<bool> TryPingAsync(this IMqttClient client, CancellationToken cancellationToken = default)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            try
            {
                await client.PingAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                // Ignore errors.
            }

            return false;
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

            var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic).Build();

            return mqttClient.UnsubscribeAsync(unsubscribeOptions, cancellationToken);
        }
    }
}