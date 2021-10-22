namespace MQTTnet.Packets
{
    public sealed class MqttConnectPacket : MqttBasePacket
    {
        public string ClientId { get; set; }

        public string Username { get; set; }

        public byte[] Password { get; set; }

        public ushort KeepAlivePeriod { get; set; }

       /// <summary>
       /// Also called "Clean Start" in MQTTv5.
       /// </summary>
        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }
        
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttConnectPacketProperties Properties { get; } = new MqttConnectPacketProperties();
        
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
