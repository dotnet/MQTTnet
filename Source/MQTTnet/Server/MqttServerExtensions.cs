using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

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

        public static IMqttServer UseApplicationMessageReceivedHandler(this IMqttServer server, Func<MqttApplicationMessageReceivedEventArgs, ValueTask> handler)
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

        public static Task SubscribeAsync(this IMqttServer server, string clientId, params TopicFilter[] topicFilters)
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

            return server.SubscribeAsync(clientId, new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task SubscribeAsync(this IMqttServer server, string clientId, string topic)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return server.SubscribeAsync(clientId, new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task UnsubscribeAsync(this IMqttServer server, string clientId, params string[] topicFilters)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return server.UnsubscribeAsync(clientId, topicFilters);
        }
    }
}
