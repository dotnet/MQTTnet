using System;

namespace MQTTnet.Core.Packets
{
    public abstract class MqttBasePacket
    {
        public TResponsePacket CreateResponse<TResponsePacket>()
        {
            var responsePacket = Activator.CreateInstance<TResponsePacket>();
            var responsePacketWithIdentifier = responsePacket as IPacketWithIdentifier;
            if (responsePacketWithIdentifier != null)
            {
                var requestPacketWithIdentifier = this as IPacketWithIdentifier;
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
