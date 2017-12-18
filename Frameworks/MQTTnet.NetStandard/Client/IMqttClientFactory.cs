using MQTTnet.Diagnostics;
using MQTTnet.ManagedClient;

namespace MQTTnet.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();

        IMqttClient CreateMqttClient(IMqttNetLogger logger);
        
        IManagedMqttClient CreateManagedMqttClient();

        IManagedMqttClient CreateManagedMqttClient(IMqttNetLogger logger);
    }
}