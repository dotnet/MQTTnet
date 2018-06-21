using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttEnqueuedApplicationMessage
    {
        public MqttEnqueuedApplicationMessage(MqttClientSession sender, MqttPublishPacket publishPacket)
        {
            Sender = sender;
            PublishPacket = publishPacket;
        }

        public MqttClientSession Sender { get; }

        public MqttPublishPacket PublishPacket { get; }
    }
}