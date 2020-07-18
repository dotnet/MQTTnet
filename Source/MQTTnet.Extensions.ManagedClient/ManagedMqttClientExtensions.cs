using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static IManagedMqttClient UseConnectedHandler(this IManagedMqttClient client, Func<MqttClientConnectedEventArgs, Task> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseConnectedHandler((IMqttClientConnectedHandler)null);
            }

            return client.UseConnectedHandler(new MqttClientConnectedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseConnectedHandler(this IManagedMqttClient client, Action<MqttClientConnectedEventArgs> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseConnectedHandler((IMqttClientConnectedHandler)null);
            }

            return client.UseConnectedHandler(new MqttClientConnectedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseConnectedHandler(this IManagedMqttClient client, IMqttClientConnectedHandler handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.ConnectedHandler = handler;
            return client;
        }

        public static IManagedMqttClient UseDisconnectedHandler(this IManagedMqttClient client, Func<MqttClientDisconnectedEventArgs, Task> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseDisconnectedHandler((IMqttClientDisconnectedHandler)null);
            }

            return client.UseDisconnectedHandler(new MqttClientDisconnectedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseDisconnectedHandler(this IManagedMqttClient client, Action<MqttClientDisconnectedEventArgs> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseDisconnectedHandler((IMqttClientDisconnectedHandler)null);
            }

            return client.UseDisconnectedHandler(new MqttClientDisconnectedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseDisconnectedHandler(this IManagedMqttClient client, IMqttClientDisconnectedHandler handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.DisconnectedHandler = handler;
            return client;
        }

        public static TReceiver UseApplicationMessageReceivedHandler<TReceiver>(this TReceiver receiver, Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
            where TReceiver : IApplicationMessageReceiver
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));

            if (handler == null)
            {
                return receiver.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return receiver.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static TReceiver UseApplicationMessageReceivedHandler<TReceiver>(this TReceiver receiver, Action<MqttApplicationMessageReceivedEventArgs> handler)
            where TReceiver : IApplicationMessageReceiver
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));

            if (handler == null)
            {
                return receiver.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return receiver.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static TReceiver UseApplicationMessageReceivedHandler<TReceiver>(this TReceiver receiver, IMqttApplicationMessageReceivedHandler handler)
            where TReceiver : IApplicationMessageReceiver
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));

            receiver.ApplicationMessageReceivedHandler = handler;
            return receiver;
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, params MqttTopicFilter[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.SubscribeAsync(topicFilters);
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, string topic)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task UnsubscribeAsync(this IManagedMqttClient client, params string[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.UnsubscribeAsync(topicFilters);
        }

        public static async Task PublishAsync(this IApplicationMessagePublisher publisher, IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await publisher.PublishAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, MqttApplicationMessage applicationMessage)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return publisher.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public static async Task PublishAsync(this IApplicationMessagePublisher publisher, params MqttApplicationMessage[] applicationMessages)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await publisher.PublishAsync(applicationMessage, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, string topic)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return publisher.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithRetainFlag(retain));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder, CancellationToken cancellationToken)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return publisher.PublishAsync(message, cancellationToken);
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IApplicationMessagePublisher publisher, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder)
        {
            if (publisher == null) throw new ArgumentNullException(nameof(publisher));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return publisher.PublishAsync(message, CancellationToken.None);
        }
    }
}
