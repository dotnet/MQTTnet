using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientFactory : IMqttClientFactory
    {
        private readonly IMqttClientAdapterFactory _mqttClientAdapterFactory;
        private readonly IMqttNetLogger _logger;

        public AspNetCoreMqttClientFactory(
            IMqttClientAdapterFactory mqttClientAdapterFactory,
            IMqttNetLogger logger)
        {
            _mqttClientAdapterFactory = mqttClientAdapterFactory;
            _logger = logger;
        }

        public IMqttClient CreateMqttClient()
        {
            return new MqttClient(_mqttClientAdapterFactory, _logger);
        }
    }
}
