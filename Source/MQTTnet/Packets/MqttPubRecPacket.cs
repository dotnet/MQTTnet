namespace MQTTnet.Packets
{
    public class MqttPubRecPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRec";
        }
    }
}
