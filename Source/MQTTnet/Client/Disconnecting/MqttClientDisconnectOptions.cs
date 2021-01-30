namespace MQTTnet.Client.Disconnecting
{
    public sealed class MqttClientDisconnectOptions
    {
        public MqttClientDisconnectReason ReasonCode { get; set; } = MqttClientDisconnectReason.NormalDisconnection;

        public string ReasonString { get; set; }
    }
}
