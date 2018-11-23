using MQTTnet.Packets;

namespace MQTTnet.Internal
{
    public static class MqttApplicationMessageExtensions
    {
        public static MqttApplicationMessage ToApplicationMessage(this MqttPublishPacket publishPacket)
        {
            return new MqttApplicationMessage
            { 
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = publishPacket.QualityOfServiceLevel,
                Retain = publishPacket.Retain
            };
        }
    }
}
