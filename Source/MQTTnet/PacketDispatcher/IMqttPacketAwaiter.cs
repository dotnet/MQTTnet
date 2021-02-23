using MQTTnet.Packets;
using System;

namespace MQTTnet.PacketDispatcher
{
    public interface IMqttPacketAwaiter : IDisposable
    {
        MqttPacketAwaiterPacketFilter PacketFilter { get; }
        
        void Complete(MqttBasePacket packet);

        void Fail(Exception exception);

        void Cancel();
    }
}