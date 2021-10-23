using System;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter
{
    public sealed class MqttSubAckPacketFactory
    {
        public MqttSubAckPacket Create(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subscribeResult == null) throw new ArgumentNullException(nameof(subscribeResult));

            var subAckPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };

            // MQTTv3.1.1
            subAckPacket.ReturnCodes.AddRange(subscribeResult.ReturnCodes);
            
            // MQTTv5.0.0
            subAckPacket.ReasonCodes.AddRange(subscribeResult.ReasonCodes);
            
            return subAckPacket;
        }
    }
}