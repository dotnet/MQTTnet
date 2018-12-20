using System;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public static class MqttClientExtensions
    {
        public static Task DisconnectAsync(this IMqttClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            return client.DisconnectAsync(null);
        }

        public static Task<MqttClientSubscribeResult> SubscribeAsync(this IMqttClient client, params TopicFilter[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return client.SubscribeAsync(topicFilters.ToList());
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

            return client.UnsubscribeAsync(topicFilters.ToList());
        }
    }
}