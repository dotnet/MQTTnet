using System;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter
{
    public sealed class MqttSubAckPacketFactory
    {
        public MqttSubAckPacket Create(MqttSubscribePacket subscribePacket, MqttSubscribeResult mqttSubscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (mqttSubscribeResult == null) throw new ArgumentNullException(nameof(mqttSubscribeResult));

            var subAckPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };

            subAckPacket.ReasonCodes.AddRange(mqttSubscribeResult.ReasonCodes);
            
            return subAckPacket;
        }
    }
}