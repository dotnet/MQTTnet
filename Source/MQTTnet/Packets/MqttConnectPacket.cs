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

        /// <summary>
        /// MQTTv5: Also called "Clean Start".
        /// </summary>
        public bool CleanSession { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }

        #region Added in MQTTv5

        public MqttConnectPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return "Connect: [ProtocolLevel=" + ProtocolLevel + "] [ClientId=" + ClientId + "] [Username=" + Username + "] [Password=" + Password + "] [KeepAlivePeriod=" + KeepAlivePeriod + "] [CleanSession=" + CleanSession + "]";
        }
    }
}
