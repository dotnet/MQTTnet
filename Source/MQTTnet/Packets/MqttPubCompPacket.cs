namespace MQTTnet.Packets
{
    public class MqttPubCompPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubComp";
        }
    }
}
