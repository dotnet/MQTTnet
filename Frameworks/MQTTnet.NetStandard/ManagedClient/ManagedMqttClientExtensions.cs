using System;
using System.Threading.Tasks;

namespace MQTTnet.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
        public static Task SubscribeAsync(this IManagedMqttClient managedClient, params TopicFilter[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.SubscribeAsync(topicFilters);
        }

        public static Task UnsubscribeAsync(this IManagedMqttClient managedClient, params TopicFilter[] topicFilters)
        {
            if (managedClient == null) throw new ArgumentNullException(nameof(managedClient));

            return managedClient.UnsubscribeAsync(topicFilters);
        }
    }
}
