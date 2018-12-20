namespace MQTTnet.Client.Connecting
{
    public class MqttClientConnectResult
    {
        public bool IsSessionPresent { get; set; }

        public MqttClientConnectResultCode ResultCode { get; set; }
    }
}
