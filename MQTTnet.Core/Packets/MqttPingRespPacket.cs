namespace MQTTnet.Core.Packets
{
    public sealed class MqttPingRespPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return nameof(MqttPingRespPacket);
        }
    }
}
