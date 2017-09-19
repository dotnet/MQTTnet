using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public static class MqttClientExtensions
    {
        public static Task PublishAsync(this IMqttClient client, params MqttApplicationMessage[] applicationMessages)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            return client.PublishAsync(applicationMessages);
        }

        public static Task<IList<MqttSubscribeResult>> SubscribeAsync(this IMqttClient client, params TopicFilter[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return client.SubscribeAsync(topicFilters.ToList());
        }

        public static Task UnsubscribeAsync(this IMqttClient client, params string[] topicFilters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return client.UnsubscribeAsync(topicFilters.ToList());
        }
    }
}