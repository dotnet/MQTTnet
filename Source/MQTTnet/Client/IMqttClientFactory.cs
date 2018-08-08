using MQTTnet.Diagnostics;

namespace MQTTnet.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();

        IMqttClient CreateMqttClient(IMqttNetLogger logger);
    }
}