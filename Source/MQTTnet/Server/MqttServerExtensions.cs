using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public static class MqttServerExtensions
    {
        public static IMqttServer UseClientConnectedHandler(this IMqttServer server, Func<MqttServerClientConnectedEventArgs, Task> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseClientConnectedHandler((IMqttServerClientConnectedHandler)null);
            }

            return server.UseClientConnectedHandler(new MqttServerClientConnectedHandlerDelegate(handler));
        }

        public static IMqttServer UseClientConnectedHandler(this IMqttServer server, Action<MqttServerClientConnectedEventArgs> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseClientConnectedHandler((IMqttServerClientConnectedHandler)null);
            }

            return server.UseClientConnectedHandler(new MqttServerClientConnectedHandlerDelegate(handler));
        }

        public static IMqttServer UseClientConnectedHandler(this IMqttServer server, IMqttServerClientConnectedHandler handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            server.ClientConnectedHandler = handler;
            return server;
        }

        public static IMqttServer UseClientDisconnectedHandler(this IMqttServer server, Func<MqttServerClientDisconnectedEventArgs, Task> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseClientDisconnectedHandler((IMqttServerClientDisconnectedHandler)null);
            }

            return server.UseClientDisconnectedHandler(new MqttServerClientDisconnectedHandlerDelegate(handler));
        }

        public static IMqttServer UseClientDisconnectedHandler(this IMqttServer server, Action<MqttServerClientDisconnectedEventArgs> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseClientDisconnectedHandler((IMqttServerClientDisconnectedHandler)null);
            }

            return server.UseClientDisconnectedHandler(new MqttServerClientDisconnectedHandlerDelegate(handler));
        }

        public static IMqttServer UseClientDisconnectedHandler(this IMqttServer server, IMqttServerClientDisconnectedHandler handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            server.ClientDisconnectedHandler = handler;
            return server;
        }

        public static IMqttServer UseApplicationMessageReceivedHandler(this IMqttServer server, Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return server.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static IMqttServer UseApplicationMessageReceivedHandler(this IMqttServer server, Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            if (handler == null)
            {
                return server.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return server.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static IMqttServer UseApplicationMessageReceivedHandler(this IMqttServer server, IMqttApplicationMessageReceivedHandler handler)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            server.ApplicationMessageReceivedHandler = handler;
            return server;
        }

        public static Task SubscribeAsync(this IMqttServer server, string clientId, params MqttTopicFilter[] topicFilters)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return server.SubscribeAsync(clientId, topicFilters);
        }

        public static Task SubscribeAsync(this IMqttServer server, string clientId, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.SubscribeAsync(clientId, new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task SubscribeAsync(this IMqttServer server, string clientId, string topic)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.SubscribeAsync(clientId, new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task UnsubscribeAsync(this IMqttServer server, string clientId, params string[] topicFilters)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return server.UnsubscribeAsync(clientId, topicFilters);
        }

        public static async Task PublishAsync(this IMqttServer server, IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await server.PublishAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, MqttApplicationMessage applicationMessage)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return server.PublishAsync(applicationMessage, CancellationToken.None);
        }

        public static async Task PublishAsync(this IMqttServer server, params MqttApplicationMessage[] applicationMessages)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                await server.PublishAsync(applicationMessage, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, string topic)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.PublishAsync(builder => builder
                .WithTopic(topic));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, string topic, string payload)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, string topic, string payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.PublishAsync(builder => builder
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfServiceLevel)
                .WithRetainFlag(retain));
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder, CancellationToken cancellationToken)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return server.PublishAsync(message, cancellationToken);
        }

        public static Task<MqttClientPublishResult> PublishAsync(this IMqttServer server, Func<MqttApplicationMessageBuilder, MqttApplicationMessageBuilder> builder)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            var message = builder(new MqttApplicationMessageBuilder()).Build();
            return server.PublishAsync(message, CancellationToken.None);
        }
    }
}
