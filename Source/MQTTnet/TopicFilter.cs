using MQTTnet.Protocol;

namespace MQTTnet
{
    public class TopicFilter
    {
        public TopicFilter(string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            Topic = topic;
            QualityOfServiceLevel = qualityOfServiceLevel;
        }

        public string Topic { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        #region Added in MQTTv5

        /// <summary>
        /// This is only available when using MQTT version 5.
        /// </summary>
        public bool? NoLocal { get; set; }

        /// <summary>
        /// This is only available when using MQTT version 5.
        /// </summary>
        public bool? RetainAsPublished { get; set; }

        /// <summary>
        /// This is only available when using MQTT version 5.
        /// </summary>
        public MqttRetainHandling? RetainHandling { get; set; }

        #endregion

        public override string ToString()
        {
            return Topic + "@" + QualityOfServiceLevel;
        }
    }
}