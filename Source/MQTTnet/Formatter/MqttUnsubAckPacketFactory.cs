using System;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter
{
    public sealed class MqttUnsubAckPacketFactory
    {
        public MqttUnsubAckPacket Create(MqttUnsubscribePacket unsubscribePacket, UnsubscribeResult unsubscribeResult)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubscribeResult == null) throw new ArgumentNullException(nameof(unsubscribeResult));

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            // MQTTv5.0.0 only.
            unsubAckPacket.ReasonCodes.AddRange(unsubscribeResult.ReasonCodes);
            
            return unsubAckPacket;
        }
    }
}