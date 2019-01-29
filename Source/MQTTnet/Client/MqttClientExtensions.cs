using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public static class MqttClientExtensions
    {
        public static IMqttClient UseReceivedApplicationMessageHandler(this IMqttClient client, Func<MqttApplicationMessageHandlerContext, Task> handler)
        {
            if (handler == null)
            {
                client.ReceivedApplicationMessageHandler = null;
                return client;
            }

            client.ReceivedApplicationMessageHandler = new MqttApplicationMessageHandlerDelegate(handler);

            return client;
        }

        public static IMqttClient UseReceivedApplicationMessageHandler(this IMqttClient client, Action<MqttApplicationMessageHandlerContext> handler)
        {
            if (handler == null)
            {
                client.ReceivedApplicationMessageHandler = null;
                return client;
            }

            client.ReceivedApplicationMessageHandler = new MqttApplicationMessageHandlerDelegate(handler);

            return client;
        }

        public static IMqttClient UseReceivedApplicationMessageHandler(this IMqttClient client, IMqttApplicationMessageHandler handler)
        {
            client.ReceivedApplicationMessageHandler = handler;

            return client;
        }

        public static Task ReconnectAsync(this IMqttClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (client.Options == null)
            {
                throw new InvalidOperationException("_ReconnectAsync_ can be used only if _ConnectAsync_ was called before.");
            }

            return client.ConnectAsync(client.Options);
        }

        public static Task DisconnectAsync(this IMqttClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.DisconnectAsync(null);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient client, params TopicFilter[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var options = new MqttClientSubscribeOptions();
            options.TopicFilters.AddRange(topicFilters);

            return client.SubscribeAsync(options);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient client, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient client, string topic)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task<MqttClientUnsubscribeResult> UnsubscribeAsync(this IMqttClient client, params string[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var options = new MqttClientUnsubscribeOptions();
            options.TopicFilters.AddRange(topicFilters);

            return client.UnsubscribeAsync(options);
        }

        public static Task<MqttClientAuthenticateResult> ConnectAsync(this IMqttClient client, IMqttClientOptions options)
        {
            return client.ConnectAsync(options, CancellationToken.None);
        }

        public static Task DisconnectAsync(this IMqttClient client, MqttClientDisconnectOptions options)
        {
            return client.DisconnectAsync(options, CancellationToken.None);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient client, MqttClientSubscribeOptions options)
        {
            return client.SubscribeAsync(options, CancellationToken.None);
        }

        public static Task<MqttClientUnsubscribeResult> UnsubscribeAsync(this IMqttClient client, MqttClientUnsubscribeOptions options)
        {
            return client.UnsubscribeAsync(options, CancellationToken.None);
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, MqttApplicationMessage applicationMessage)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return client.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public static async Task PublishAsync(this IMqttClient client, IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await client.PublishAsync(applicationMessage).ConfigureAwait(false);
            }
        }
        
        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .Build());
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic, IEnumerable<byte> payload)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build());
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic, string payload)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build());
        }
        
        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .Build());
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithRetainFlag(retain)
                .Build());
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttClient client, string topic, string payload, bool retain)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build());
        }
    }
}