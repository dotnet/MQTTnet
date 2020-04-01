using MQTTnet.Protocol;

namespace MQTTnet
{
    // TODO: Consider renaming to "MqttTopicFilter"
    public class TopicFilter
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