namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacket : MqttBasePacket
    {
        public string ClientId { get; set; }

        public string Username { get; set; }

        public byte[] Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

        // Also called "Clean Start" in MQTTv5.
        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }

        #region Added in MQTTv5.0.0

        public MqttConnectPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var passwordText = string.Empty;

            if (Password != null)
            {
                passwordText = "****";
            }

            return string.Concat("Connect: [ClientId=", ClientId, "] [Username=", Username, "] [Password=", passwordText, "] [KeepAlivePeriod=", KeepAlivePeriod, "] [CleanSession=", CleanSession, "]");
        }
    }
}
