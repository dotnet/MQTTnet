using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.ManagedClient;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateMqttClient();

        IMqttClient CreateMqttClient(IMqttNetLogger logger);
        
        IManagedMqttClient CreateManagedMqttClient();

        IManagedMqttClient CreateManagedMqttClient(IMqttNetLogger logger);
    }
}