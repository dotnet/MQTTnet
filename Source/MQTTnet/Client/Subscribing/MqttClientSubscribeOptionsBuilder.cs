using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeOptionsBuilder
    {
        private readonly MqttClientSubscribeOptions _subscribeOptions = new MqttClientSubscribeOptions();

        public MqttClientSubscribeOptionsBuilder WithUserProperty(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (_subscribeOptions.UserProperties == null)
            {
                _subscribeOptions.UserProperties = new List<MqttUserProperty>();
            }

            _subscribeOptions.UserProperties.Add(new MqttUserProperty(name, value));

            return this;
        }

        public MqttClientSubscribeOptionsBuilder WithSubscriptionIdentifier(uint? subscriptionIdentifier)
        {
            _subscribeOptions.SubscriptionIdentifier = subscriptionIdentifier;

            return this;
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool? noLocal = null,
            bool? retainAsPublished = null,
            MqttRetainHandling? retainHandling = null)
        {
            return WithTopicFilter(new TopicFilter
            {
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                NoLocal = noLocal,
                RetainAsPublished = retainAsPublished,
                RetainHandling = retainHandling
            });
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(Action<TopicFilterBuilder> topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            var internalTopicFilterBuilder = new TopicFilterBuilder();
            topicFilterBuilder(internalTopicFilterBuilder);

            return WithTopicFilter(internalTopicFilterBuilder);
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(TopicFilterBuilder topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            return WithTopicFilter(topicFilterBuilder.Build());
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(TopicFilter topicFilter)
        {
            if (topicFilter == null) throw new ArgumentNullException(nameof(topicFilter));

            if (_subscribeOptions.TopicFilters == null)
            {
                _subscribeOptions.TopicFilters = new List<TopicFilter>();
            }

            _subscribeOptions.TopicFilters.Add(topicFilter);

            return this;
        }

        public MqttClientSubscribeOptions Build()
        {
            return _subscribeOptions;
        }
    }
}
