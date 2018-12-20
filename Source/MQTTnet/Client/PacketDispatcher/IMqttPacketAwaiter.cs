using System;
using MQTTnet.Packets;

namespace MQTTnet.Client.PacketDispatcher
{
    public interface IMqttPacketAwaiter
    {
        void Complete(MqttBasePacket packet);

        void Fail(Exception exception);
    }
}