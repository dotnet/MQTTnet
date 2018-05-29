using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttSubscribeResult
    {
        public MqttSubAckPacket ResponsePacket { get; set; }

        public bool CloseConnection { get; set; }
    }
}
