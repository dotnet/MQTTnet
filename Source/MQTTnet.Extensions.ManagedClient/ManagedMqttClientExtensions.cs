using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

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

        public static IManagedMqttClient UseApplicationMessageReceivedHandler(this IManagedMqttClient client, Func<MqttApplicationMessageReceivedEventArgs, ValueTask> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return client.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseApplicationMessageReceivedHandler(this IManagedMqttClient client, Action<MqttApplicationMessageReceivedEventArgs> handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            if (handler == null)
            {
                return client.UseApplicationMessageReceivedHandler((IMqttApplicationMessageReceivedHandler)null);
            }

            return client.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(handler));
        }

        public static IManagedMqttClient UseApplicationMessageReceivedHandler(this IManagedMqttClient client, IMqttApplicationMessageReceivedHandler handler)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            client.ApplicationMessageReceivedHandler = handler;
            return client;
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, params TopicFilter[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.SubscribeAsync(topicFilters);
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
        }

        public static Task SubscribeAsync(this IManagedMqttClient client, string topic)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            return client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public static Task UnsubscribeAsync(this IManagedMqttClient client, params string[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.UnsubscribeAsync(topicFilters);
        }
    }
}
