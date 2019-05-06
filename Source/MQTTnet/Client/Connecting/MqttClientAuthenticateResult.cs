namespace MQTTnet.Client.Connecting
{
    public class MqttClientAuthenticateResult
    {
        public bool IsSessionPresent { get; set; }

        public MqttClientConnectResultCode ResultCode { get; set; }
    }
}
