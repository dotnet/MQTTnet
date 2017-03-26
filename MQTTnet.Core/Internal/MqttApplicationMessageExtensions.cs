using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Internal
{
    internal static class MqttApplicationMessageExtensions
    {
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
