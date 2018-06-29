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

        public static MqttPublishPacket ToPublishPacket(this MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                return null;
            }

            return new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false
            };
        }
    }
}
