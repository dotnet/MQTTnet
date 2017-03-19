namespace MQTTnet.Core.Packets
{
    public class MqttPingRespPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return nameof(MqttPingRespPacket);
        }
    }
}
