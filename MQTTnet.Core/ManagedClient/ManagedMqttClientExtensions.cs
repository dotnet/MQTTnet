using System;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static Task EnqueueAsync(this ManagedMqttClient managedClient, params MqttApplicationMessage[] applicationMessages)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.EnqueueAsync(applicationMessages);
        }

        public static Task SubscribeAsync(this ManagedMqttClient managedClient, params TopicFilter[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.SubscribeAsync(topicFilters);
        }

        public static Task UnsubscribeAsync(this ManagedMqttClient managedClient, params TopicFilter[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.UnsubscribeAsync(topicFilters);
        }
    }
}
