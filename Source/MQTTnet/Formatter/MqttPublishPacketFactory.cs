using System;
using MQTTnet.Exceptions;
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

        public MqttPublishPacket Create(MqttConnectPacket connectPacket)
        {
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));

            if (!connectPacket.WillFlag)
            {
                throw new MqttProtocolViolationException("The CONNECT packet contains no will message (WillFlag).");
            }
            
            var packet = new MqttPublishPacket
            {
                Topic = connectPacket.WillTopic,
                Payload = connectPacket.WillMessage,
                QualityOfServiceLevel = connectPacket.WillQoS,
                Retain = connectPacket.WillRetain,
                Dup = false,
                Properties =
                {
                    ContentType = connectPacket.WillProperties.ContentType,
                    CorrelationData = connectPacket.WillProperties.CorrelationData,
                    MessageExpiryInterval = connectPacket.WillProperties.MessageExpiryInterval,
                    PayloadFormatIndicator = connectPacket.WillProperties.PayloadFormatIndicator,
                    ResponseTopic = connectPacket.WillProperties.ResponseTopic
                }
            };
            
            packet.Properties.UserProperties.AddRange(connectPacket.WillProperties.UserProperties);

            return packet;
        }
    }
}