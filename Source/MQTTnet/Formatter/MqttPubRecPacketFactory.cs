


using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubRecPacketFactory
    {
        public MqttPubRecPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
        {
            if (applicationMessageReceivedEventArgs == null) throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));

            var pubRecPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubRecPacket.Properties.UserProperties.AddRange(applicationMessageReceivedEventArgs.ResponseUserProperties);
            
            return pubRecPacket;
        }

        public MqttPubRecPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));
            
            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode) (int) applicationMessageReceivedReasonCode
            };
            
            return pubRecPacket;
        }
    }
}