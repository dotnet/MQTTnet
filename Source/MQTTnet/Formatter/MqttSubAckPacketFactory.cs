using System;
using MQTTnet.Packets;
using MQTTnet.Server;

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

            subAckPacket.ReasonCodes.AddRange(subscribeResult.ReasonCodes);
            
            return subAckPacket;
        }
    }
}