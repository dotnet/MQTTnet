namespace MQTTnet.Client.Disconnecting
{
    public class MqttClientDisconnectOptions
    {
        public MqttClientDisconnectReason ReasonCode { get; set; } = MqttClientDisconnectReason.NormalDisconnection;

        public string ReasonString { get; set; }
    }
}
