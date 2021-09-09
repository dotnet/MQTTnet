using MQTTnet.Packets;
using System;

namespace MQTTnet.PacketDispatcher
{
    public interface IMqttPacketAwaitable : IDisposable
    {
        MqttPacketAwaitableFilter Filter { get; }
        
        void Complete(MqttBasePacket packet);

        void Fail(Exception exception);

        void Cancel();
    }
}