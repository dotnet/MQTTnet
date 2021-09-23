using System;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaitableFilter
    {
        public Type Type { get; set; }
        
        public ushort Identifier { get; set; }
    }
}