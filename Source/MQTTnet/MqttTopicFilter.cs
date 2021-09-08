using MQTTnet.Protocol;
using System;

namespace MQTTnet
{
    [Obsolete("Use MqttTopicFilter instead. It is just a renamed version to align with general namings in this lib.")]
    public class TopicFilter : MqttTopicFilter
    {
    }

    // TODO: Consider using struct instead.
    public class MqttTopicFilter
    {
        /// <summary>
        /// Gets or sets the MQTT topic.
        /// In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected client.
        /// The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level separator). 
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the quality of service level.
        /// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message that defines the guarantee of delivery for a specific message.
        /// There are 3 QoS levels in MQTT:
        /// - At most once  (0): Message gets delivered no time, once or multiple times.
        /// - At least once (1): Message gets delivered at least once (one time or more often).
        /// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
        /// </summary>
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to not send messages originating on this client (noLocal) or not.
        /// </summary>
        /// Hint: MQTT 5 feature only.
        public bool? NoLocal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether messages are retained as published or not.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public bool? RetainAsPublished { get; set; }

        /// <summary>
        /// Gets or sets the retain handling.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public MqttRetainHandling? RetainHandling { get; set; }

        public override string ToString()
        {
            return string.Concat(
                "TopicFilter: [Topic=",
                Topic,
                "] [QualityOfServiceLevel=",
                QualityOfServiceLevel,
                "] [NoLocal=",
                NoLocal,
                "] [RetainAsPublished=",
                RetainAsPublished,
                "] [RetainHandling=",
                RetainHandling,
                "]");
        }
    }
}