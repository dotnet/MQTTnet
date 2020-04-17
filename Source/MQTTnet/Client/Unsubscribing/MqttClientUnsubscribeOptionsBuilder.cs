using MQTTnet.Packets;
using System;
using System.Collections.Generic;

namespace MQTTnet.Client.Unsubscribing
{
    public class MqttClientUnsubscribeOptionsBuilder
    {
        private readonly MqttClientUnsubscribeOptions _unsubscribeOptions = new MqttClientUnsubscribeOptions();

        public MqttClientUnsubscribeOptionsBuilder WithUserProperty(string name, string value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (value is null) throw new ArgumentNullException(nameof(value));

            return WithUserProperty(new MqttUserProperty(name, value));
        }

        public MqttClientUnsubscribeOptionsBuilder WithUserProperty(MqttUserProperty userProperty)
        {
            if (userProperty is null) throw new ArgumentNullException(nameof(userProperty));

            if (_unsubscribeOptions.UserProperties is null)
            {
                _unsubscribeOptions.UserProperties = new List<MqttUserProperty>();
            }

            _unsubscribeOptions.UserProperties.Add(userProperty);

            return this;
        }

        public MqttClientUnsubscribeOptionsBuilder WithTopicFilter(string topic)
        {
            if (topic is null) throw new ArgumentNullException(nameof(topic));

            if (_unsubscribeOptions.TopicFilters is null)
            {
                _unsubscribeOptions.TopicFilters = new List<string>();
            }

            _unsubscribeOptions.TopicFilters.Add(topic);

            return this;
        }

        public MqttClientUnsubscribeOptionsBuilder WithTopicFilter(MqttTopicFilter topicFilter)
        {
            if (topicFilter is null) throw new ArgumentNullException(nameof(topicFilter));

            return WithTopicFilter(topicFilter.Topic);
        }

        public MqttClientUnsubscribeOptions Build()
        {
            return _unsubscribeOptions;
        }
    }
}
