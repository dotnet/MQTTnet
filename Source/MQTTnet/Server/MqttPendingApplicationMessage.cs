using MQTTnet.Packets;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttPendingApplicationMessage
    {
        public MqttPendingApplicationMessage(MqttPublishPacket publishPacket, MqttClientConnection sender)
        {
            Sender = sender;
            PublishPacket = publishPacket;
        }

        public MqttClientConnection Sender { get; }

        public MqttPublishPacket PublishPacket { get; }
    }
}