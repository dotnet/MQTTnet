using System;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.LowLevelClient;

namespace MQTTnet.Client
{
    public interface IMqttClientFactory
    {
        IMqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory);

        ILowLevelMqttClient CreateLowLevelMqttClient();

        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger);

        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory);

        ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory);

        IMqttClient CreateMqttClient();

        IMqttClient CreateMqttClient(IMqttNetLogger logger);

        IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory);

        IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory);
    }
}