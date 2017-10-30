using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public class MqttSubscribeResult
    {
        public MqttSubAckPacket ResponsePacket { get; set; }

        public bool CloseConnection { get; set; }
    }
}
