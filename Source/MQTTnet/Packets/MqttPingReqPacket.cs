namespace MQTTnet.Packets
{
    public sealed class MqttPingReqPacket : MqttBasePacket
    {
        // This is a minor performance improvement.
        public static readonly MqttPingReqPacket Instance = new MqttPingReqPacket();

        public override string ToString()
        {
            return "PingReq";
        }
    }
}
