using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubRelPacketFactory
    {
        public MqttPubRelPacket Create(MqttPubRecPacket pubRecPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRecPacket == null) throw new ArgumentNullException(nameof(pubRecPacket));

            return new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier,
                ReasonCode = (MqttPubRelReasonCode) (int) reasonCode
            };
        }
    }
}