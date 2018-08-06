namespace MQTTnet.Packets.V3
{
    public class MqttV3ConnectPacket : MqttConnectPacket
    {
        public string ProtocolName { get; set; }

        public byte ProtocolLevel { get; set; }

        public string ClientId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }

        public override string ToString()
        {
            return "Connect: [ProtocolLevel=" + ProtocolLevel + "] [ClientId=" + ClientId + "] [Username=" + Username + "] [Password=" + Password + "] [KeepAlivePeriod=" + KeepAlivePeriod + "] [CleanSession=" + CleanSession + "]";
        }
    }
}
