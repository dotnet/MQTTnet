namespace MQTTnet.Packets
{
    public sealed class MqttPingRespPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return "PingResp";
        }
    }
}
