namespace MQTTnet.Packets
{
    public class MqttConnectPacket : MqttBasePacket
    {
        public string ProtocolName { get; set; }

        public byte? ProtocolLevel { get; set; }

        public string ClientId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

        // Also called "Clean Start" in MQTTv5.
        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }

        #region Added in MQTTv5.0.0

        public MqttConnectPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var password = Password;
            if (!string.IsNullOrEmpty(password))
            {
                password = "****";
            }

            return string.Concat("Connect: [ProtocolLevel=", ProtocolLevel, "] [ClientId=", ClientId, "] [Username=", Username, "] [Password=", password, "] [KeepAlivePeriod=", KeepAlivePeriod, "] [CleanSession=", CleanSession, "]");
        }
    }
}
