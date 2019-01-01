using System;
using System.Threading.Tasks;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public static class MqttServerExtensions
    {
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
