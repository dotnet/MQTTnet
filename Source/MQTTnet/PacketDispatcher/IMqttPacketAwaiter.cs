using System;
using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher
{
    public interface IMqttPacketAwaiter : IDisposable
    {
        void Complete(MqttBasePacket packet);

        void Fail(Exception exception);

        void Cancel();
    }
}