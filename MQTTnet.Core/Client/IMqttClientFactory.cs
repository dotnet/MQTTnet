namespace MQTTnet.Core.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();

        IMqttClientQueued CreateMqttQueuedClient();
    }
}