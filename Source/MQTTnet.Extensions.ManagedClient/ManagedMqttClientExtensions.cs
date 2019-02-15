using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static IManagedMqttClient UseReceivedApplicationMessageHandler(this IManagedMqttClient client, Func<MqttApplicationMessageHandlerContext, Task> handler)
        {
            if (handler == null)
            {
                client.ReceivedApplicationMessageHandler = null;
                return client;
            }

            client.ReceivedApplicationMessageHandler = new MqttApplicationMessageHandlerDelegate(handler);

            return client;
        }

        public static IManagedMqttClient UseReceivedApplicationMessageHandler(this IManagedMqttClient client, Action<MqttApplicationMessageHandlerContext> handler)
        {
            if (handler == null)
            {
                client.ReceivedApplicationMessageHandler = null;
                return client;
            }

            client.ReceivedApplicationMessageHandler = new MqttApplicationMessageHandlerDelegate(handler);

            return client;
        }

        public static Task SubscribeAsync(this IManagedMqttClient managedClient, params TopicFilter[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.SubscribeAsync(topicFilters);
        }

        public static Task SubscribeAsync(this IManagedMqttClient managedClient, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return managedClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task SubscribeAsync(this IManagedMqttClient managedClient, string topic)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return managedClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task UnsubscribeAsync(this IManagedMqttClient managedClient, params string[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.UnsubscribeAsync(topicFilters);
        }

        public static async Task PublishAsync(this IManagedMqttClient client, IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await client.PublishAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, MqttApplicationMessage applicationMessage)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return client.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public static async Task PublishAsync(this IManagedMqttClient client, params MqttApplicationMessage[] applicationMessages)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await client.PublishAsync(applicationMessage, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, string topic)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(builder => builder
                .WithTopic(topic));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, string topic, string payload)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithRetainFlag(retain));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder, CancellationToken cancellationToken)
        {
            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return client.PublishAsync(message, cancellationToken);
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IManagedMqttClient client, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder)
        {
            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return client.PublishAsync(message, CancellationToken.None);
        }
    }
}
