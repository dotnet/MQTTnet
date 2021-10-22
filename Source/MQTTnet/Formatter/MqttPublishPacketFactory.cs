using System;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttPublishPacketFactory
    {
        public MqttPublishPacket Create(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            // Copy all values to their matching counterparts.
            // The not supported values in MQTT 3.1.1 are not serialized (excluded) later.
            var packet = new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = applicationMessage.Dup,
                Properties =
                {
                    ContentType = applicationMessage.ContentType,
                    CorrelationData = applicationMessage.CorrelationData,
                    MessageExpiryInterval = applicationMessage.MessageExpiryInterval,
                    PayloadFormatIndicator = applicationMessage.PayloadFormatIndicator,
                    ResponseTopic = applicationMessage.ResponseTopic,
                    TopicAlias = applicationMessage.TopicAlias
                }
            };

            if (applicationMessage.SubscriptionIdentifiers != null)
            {
                packet.Properties.SubscriptionIdentifiers.AddRange(applicationMessage.SubscriptionIdentifiers);    
            }
            
            if (applicationMessage.UserProperties != null)
            {
                packet.Properties.UserProperties.AddRange(applicationMessage.UserProperties);
            }

            return packet;
        }
    }
}