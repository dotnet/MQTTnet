using MQTTnet.Protocol;
using System;

namespace MQTTnet
{
    [Obsolete("Use MqttTopicFilter instead. It is just a renamed version to align with general namings in this lib.")]
    public class TopicFilter : MqttTopicFilter
    {
    }

    public class MqttTopicFilter
    {
        public string Topic { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public bool? NoLocal { get; set; }

        /// <summary>
        /// This is only supported when using MQTTv5.
        /// </summary>
        public bool? RetainAsPublished { get; set; }

        /// <summary>
        /// This is only supported when using MQTTv5.
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