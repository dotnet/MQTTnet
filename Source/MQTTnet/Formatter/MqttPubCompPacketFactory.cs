using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubCompPacketFactory
    {
        public MqttPubCompPacket Create(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRelPacket == null) throw new ArgumentNullException(nameof(pubRelPacket));

            return new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = (MqttPubCompReasonCode) (int) reasonCode
            };
        }
    }
}