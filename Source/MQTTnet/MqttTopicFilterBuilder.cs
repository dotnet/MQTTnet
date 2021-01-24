using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System;

namespace MQTTnet
{
    [Obsolete("Use MqttTopicFilterBuilder instead. It is just a renamed version to align with general namings in this lib.")]
    public class TopicFilterBuilder : MqttTopicFilterBuilder
    {
    }

    public class MqttTopicFilterBuilder
    {
        MqttQualityOfServiceLevel _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
        string _topic;
        bool? _noLocal;
        bool? _retainAsPublished;
        MqttRetainHandling? _retainHandling;

        public MqttTopicFilterBuilder WithTopic(string topic)
        {
            _topic = topic;
            return this;
        }

        public MqttTopicFilterBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            return this;
        }

        public MqttTopicFilterBuilder WithAtLeastOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            return this;
        }

        public MqttTopicFilterBuilder WithAtMostOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            return this;
        }

        public MqttTopicFilterBuilder WithExactlyOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            return this;
        }

        public MqttTopicFilterBuilder WithNoLocal(bool? value = true)
        {
            _noLocal = value;
            return this;
        }

        public MqttTopicFilterBuilder WithRetainAsPublished(bool? value = true)
        {
            _retainAsPublished = value;
            return this;
        }

        public MqttTopicFilterBuilder WithRetainHandling(MqttRetainHandling? value)
        {
            _retainHandling = value;
            return this;
        }
        
        public MqttTopicFilter Build()
        {
            if (string.IsNullOrEmpty(_topic))
            {
                throw new MqttProtocolViolationException("Topic is not set.");
            }

            return new MqttTopicFilter
            {
                Topic = _topic, 
                QualityOfServiceLevel = _qualityOfServiceLevel,
                NoLocal = _noLocal,
                RetainAsPublished = _retainAsPublished,
                RetainHandling = _retainHandling
            };
        }
    }
}
