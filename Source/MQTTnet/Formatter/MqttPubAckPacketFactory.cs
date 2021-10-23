using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubAckPacketFactory
    {
        public MqttPubAckPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
        {
            if (applicationMessageReceivedEventArgs == null) throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));

            var pubAckPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubAckPacket.Properties.UserProperties.AddRange(applicationMessageReceivedEventArgs.ResponseUserProperties);
            
            return pubAckPacket;
        }

        public MqttPubAckPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
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