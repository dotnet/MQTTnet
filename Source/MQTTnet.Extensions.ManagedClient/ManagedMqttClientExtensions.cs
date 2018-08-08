using System;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class ManagedMqttClientExtensions
    {
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
    }
}
