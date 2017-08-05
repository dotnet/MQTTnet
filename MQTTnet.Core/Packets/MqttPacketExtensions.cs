using System;

namespace MQTTnet.Core.Packets
{
    public static class MqttPacketExtensions
    {
        public static TResponsePacket CreateResponse<TResponsePacket>(this MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var responsePacket = Activator.CreateInstance<TResponsePacket>();

            if (responsePacket is IMqttPacketWithIdentifier responsePacketWithIdentifier)
            {
                var requestPacketWithIdentifier = packet as IMqttPacketWithIdentifier;
                if (requestPacketWithIdentifier == null)
                {
                    throw new InvalidOperationException("Response packet has PacketIdentifier but request packet does not.");
                }

                responsePacketWithIdentifier.PacketIdentifier = requestPacketWithIdentifier.PacketIdentifier;
            }

            return responsePacket;
        }
    }
}
