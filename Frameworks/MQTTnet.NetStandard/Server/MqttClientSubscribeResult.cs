using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttClientSubscribeResult
    {
        public MqttSubAckPacket ResponsePacket { get; set; }

        public bool CloseConnection { get; set; }
    }
}
