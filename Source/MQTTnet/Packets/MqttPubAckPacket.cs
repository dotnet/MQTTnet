namespace MQTTnet.Packets
{
    public class MqttPubAckPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return $"PubAck [PacketIdentifier={PacketIdentifier}]";
        }
    }
}
