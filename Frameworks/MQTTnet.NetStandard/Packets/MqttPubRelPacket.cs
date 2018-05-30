namespace MQTTnet.Packets
{
    public class MqttPubRelPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRel";
        }
    }
}
