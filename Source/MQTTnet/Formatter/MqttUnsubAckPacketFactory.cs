using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttUnsubAckPacketFactory
    {
        public MqttUnsubAckPacket Create(MqttUnsubscribePacket unsubscribePacket, MqttUnsubscribeResult mqttUnsubscribeResult)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (mqttUnsubscribeResult == null) throw new ArgumentNullException(nameof(mqttUnsubscribeResult));

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            // MQTTv5.0.0 only.
            unsubAckPacket.ReasonCodes.AddRange(mqttUnsubscribeResult.ReasonCodes);
            
            return unsubAckPacket;
        }
    }
}