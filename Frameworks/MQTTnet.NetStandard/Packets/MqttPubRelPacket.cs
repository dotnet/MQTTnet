namespace MQTTnet.Packets
{
    public sealed class MqttPubRelPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRel";
        }
    }
}
