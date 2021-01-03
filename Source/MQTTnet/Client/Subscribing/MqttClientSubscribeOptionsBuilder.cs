using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeOptionsBuilder
    {
        private readonly MqttClientSubscribeOptions _subscribeOptions = new MqttClientSubscribeOptions();

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

        /// <summary>
        /// Adds the subscription identifier to the subscribe options.
        /// </summary>
        /// <param name="subscriptionIdentifier">The subscription identifier.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttClientSubscribeOptionsBuilder WithSubscriptionIdentifier(uint? subscriptionIdentifier)
        {
            _subscribeOptions.SubscriptionIdentifier = subscriptionIdentifier;
            return this;
        }

        /// <summary>
        /// Adds a topic filter to the subscribe options.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="qualityOfServiceLevel">The quality of service level.</param>
        /// <param name="noLocal">A value indicating whether to not send messages originating on this client (noLocal) or not.</param>
        /// <param name="retainAsPublished">A value indicating whether messages are retained as published or not.</param>
        /// <param name="retainHandling">The retain handling.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttClientSubscribeOptionsBuilder WithTopicFilter(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            bool? noLocal = null,
            bool? retainAsPublished = null,
            MqttRetainHandling? retainHandling = null)
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

        /// <summary>
        /// Adds the topic filter to the subscribe options.
        /// </summary>
        /// <param name="topicFilterBuilder">The topic filter builder.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttClientSubscribeOptionsBuilder WithTopicFilter(Action<MqttTopicFilterBuilder> topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            var internalTopicFilterBuilder = new MqttTopicFilterBuilder();
            topicFilterBuilder(internalTopicFilterBuilder);

            return WithTopicFilter(internalTopicFilterBuilder);
        }

        /// <summary>
        /// Adds the topic filter to the subscribe options.
        /// </summary>
        /// <param name="topicFilterBuilder">The topic filter builder.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
        public MqttClientSubscribeOptionsBuilder WithTopicFilter(MqttTopicFilterBuilder topicFilterBuilder)
        {
            if (topicFilterBuilder == null) throw new ArgumentNullException(nameof(topicFilterBuilder));

            return WithTopicFilter(topicFilterBuilder.Build());
        }

        /// <summary>
        /// Adds the topic filter to the subscribe options.
        /// </summary>
        /// <param name="topicFilter">The topic filter.</param>
        /// <returns>A new instance of the <see cref="MqttApplicationMessageBuilder"/> class.</returns>
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

        /// <summary>
        /// Builds the subscription options.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttClientSubscribeOptions"/> class.</returns>
        public MqttClientSubscribeOptions Build()
        {
            return _subscribeOptions;
        }
    }
}
