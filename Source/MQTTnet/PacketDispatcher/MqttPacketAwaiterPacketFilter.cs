using System;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaiterPacketFilter
    {
        public Type Type { get; set; }
        
        public ushort Identifier { get; set; }
    }
}