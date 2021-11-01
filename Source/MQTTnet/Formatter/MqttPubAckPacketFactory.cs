using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubAckPacketFactory
    {
        public MqttPubAckPacket Create(MqttPublishPacket publishPacket, PublishResponse applicationMessageResponse)
        {
            if (applicationMessageResponse == null) throw new ArgumentNullException(nameof(applicationMessageResponse));

            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var pubAckPacket = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubAckReasonCode) (int) applicationMessageResponse.ReasonCode,
                Properties =
                {
                    ReasonString = applicationMessageResponse.ReasonString
                }
            };

            pubAckPacket.Properties.UserProperties.AddRange(applicationMessageResponse.UserProperties);
            
            return pubAckPacket;
        }
        
        public MqttPubAckPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
        {
            if (applicationMessageReceivedEventArgs == null) throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));

            var pubAckPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubAckPacket.Properties.UserProperties.AddRange(applicationMessageReceivedEventArgs.ResponseUserProperties);
            pubAckPacket.Properties.ReasonString = applicationMessageReceivedEventArgs.ResponseReasonString;

            return pubAckPacket;
        }

        static MqttPubAckPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var pubAckPacket = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubAckReasonCode) (int) applicationMessageReceivedReasonCode
            };

            return pubAckPacket;
        }
    }
}