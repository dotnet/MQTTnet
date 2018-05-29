namespace MQTTnet.Packets
{
    public sealed class MqttPingReqPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return "PingReq";
        }
    }
}
