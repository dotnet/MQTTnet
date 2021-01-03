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
        /// <summary>
        /// The quality of service level.
        /// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message.
        /// There are 3 QoS levels in MQTT:
        /// - At most once  (0): Message gets delivered no time, once or multiple times.
        /// - At least once (1): Message gets delivered at least once (one time or more often).
        /// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        MqttQualityOfServiceLevel _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;

        /// <summary>
        /// The MQTT topic.
        /// In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected client.
        /// The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level separator).
        /// </summary>
        string _topic;

        /// <summary>
        /// Adds a topic to the topic filter.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>A new instance of the <see cref="MqttTopicFilterBuilder"/> class.</returns>
        public MqttTopicFilterBuilder WithTopic(string topic)
        {
            _topic = topic;
            return this;
        }

        /// <summary>
        /// Adds the quality of service level to the topic filter.
        /// </summary>
        /// <param name="qualityOfServiceLevel">The quality of service level.</param>
        /// <returns>A new instance of the <see cref="MqttTopicFilterBuilder"/> class.</returns>
        public MqttTopicFilterBuilder WithQualityOfServiceLevel(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            _qualityOfServiceLevel = qualityOfServiceLevel;
            return this;
        }

        /// <summary>
        /// Adds the quality of service level 0 (at least once) to the topic filter.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttTopicFilterBuilder"/> class.</returns>
        public MqttTopicFilterBuilder WithAtLeastOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            return this;
        }

        /// <summary>
        /// Adds the quality of service level 1 (at most once) to the topic filter.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttTopicFilterBuilder"/> class.</returns>
        public MqttTopicFilterBuilder WithAtMostOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            return this;
        }

        /// <summary>
        /// Adds the quality of service level 2 (exactly once) to the topic filter.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttTopicFilterBuilder"/> class.</returns>
        public MqttTopicFilterBuilder WithExactlyOnceQoS()
        {
            _qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            return this;
        }

        /// <summary>
        /// Builds the topic filter.
        /// </summary>
        /// <returns>A new instance of the <see cref="MqttTopicFilter"/> class.</returns>
        public MqttTopicFilter Build()
        {
            if (string.IsNullOrEmpty(_topic))
            {
                throw new MqttProtocolViolationException("Topic is not set.");
            }

            return new MqttTopicFilter { Topic = _topic, QualityOfServiceLevel = _qualityOfServiceLevel };
        }
    }
}
