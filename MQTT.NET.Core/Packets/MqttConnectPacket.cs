namespace MQTTnet.Core.Packets
{
    public class MqttConnectPacket: MqttBasePacket
    {
        public string ClientId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }
    }
}
