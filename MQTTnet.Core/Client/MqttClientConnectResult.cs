namespace MQTTnet.Core.Client
{
    public class MqttClientConnectResult
    {
        public MqttClientConnectResult(bool isSessionPresent)
        {
            IsSessionPresent = isSessionPresent;
        }

        public bool IsSessionPresent { get; }
    }
}
