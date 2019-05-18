using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Client
{
    public interface IMqttClientFactory
    {
        IMqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory);

        IMqttClient CreateMqttClient();

        IMqttClient CreateMqttClient(IMqttNetLogger logger);

        IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory);

        IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory);
    }
}