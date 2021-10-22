using System;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttApplicationMessageFactory
    {
        public MqttApplicationMessage Create(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttApplicationMessage
            {
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = publishPacket.QualityOfServiceLevel,
                Retain = publishPacket.Retain,
                Dup = publishPacket.Dup,
                ResponseTopic = publishPacket.Properties.ResponseTopic,
                ContentType = publishPacket.Properties.ContentType,
                CorrelationData = publishPacket.Properties.CorrelationData,
                MessageExpiryInterval = publishPacket.Properties.MessageExpiryInterval,
                SubscriptionIdentifiers = publishPacket.Properties.SubscriptionIdentifiers,
                TopicAlias = publishPacket.Properties.TopicAlias,
                PayloadFormatIndicator = publishPacket.Properties.PayloadFormatIndicator,
                UserProperties = publishPacket.Properties.UserProperties
            };
        }
    }
}