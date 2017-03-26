using System;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public class MqttPacketAwaiter : TaskCompletionSource<MqttBasePacket>
    {
        public MqttPacketAwaiter(Func<MqttBasePacket, bool> packetSelector)
        {
            PacketSelector = packetSelector ?? throw new ArgumentNullException(nameof(packetSelector));
        }

        public Func<MqttBasePacket, bool> PacketSelector { get; }
    }
}