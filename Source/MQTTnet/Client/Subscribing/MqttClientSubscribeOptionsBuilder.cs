using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using MQTTnet.Exceptions;

namespace MQTTnet.Client.Subscribing
{
    public sealed class MqttClientSubscribeOptionsBuilder
    {
        readonly MqttClientSubscribeOptions _subscribeOptions = new MqttClientSubscribeOptions();

        /// <summary>
        /// Adds the user property to the subscribe options.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
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

        public MqttClientSubscribeOptionsBuilder WithSubscriptionIdentifier(uint subscriptionIdentifier)
        {
            if (subscriptionIdentifier == 0)
            {
                throw new MqttProtocolViolationException("Subscription identifier cannot be 0.");
            }
            
            _subscribeOptions.SubscriptionIdentifier = subscriptionIdentifier;
            return this;
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool noLocal = false,
            bool retainAsPublished = false,
            MqttRetainHandling retainHandling = MqttRetainHandling.SendAtSubscribe)
        {
            return WithTopicFilter(new MqttTopicFilter
            {
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                NoLocal = noLocal,
                RetainAsPublished = retainAsPublished,
                RetainHandling = retainHandling
            });
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(Action<MqttTopicFilterBuilder> topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            var internalTopicFilterBuilder = new MqttTopicFilterBuilder();
            topicFilterBuilder(internalTopicFilterBuilder);

            return WithTopicFilter(internalTopicFilterBuilder);
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(MqttTopicFilterBuilder topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            return WithTopicFilter(topicFilterBuilder.Build());
        }

        public MqttClientSubscribeOptionsBuilder WithTopicFilter(MqttTopicFilter topicFilter)
        {
            if (topicFilter == null) throw new ArgumentNullException(nameof(topicFilter));

            if (_subscribeOptions.TopicFilters == null)
            {
                _subscribeOptions.TopicFilters = new List<MqttTopicFilter>();
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
