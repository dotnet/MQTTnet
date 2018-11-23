using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public class MqttPublishPacketProperties
    {
        public byte? PayloadFormatIndicator { get; set; }

        public uint? MessageExpiryInterval { get; set; }

        public ushort? TopicAlias { get; set; }

        public string ResponseTopic { get; set; }

        public byte[] CorrelationData { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public uint? SubscriptionIdentifier { get; set; }

        public string ContentType { get; set; }
    }
}
