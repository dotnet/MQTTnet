using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public class MqttClientSubscribeResult
    {
        public MqttSubAckPacket ResponsePacket { get; set; }

        public bool CloseConnection { get; set; }
    }
}
