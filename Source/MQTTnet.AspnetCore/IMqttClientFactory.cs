namespace MQTTnet.AspNetCore
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();
    }
}
