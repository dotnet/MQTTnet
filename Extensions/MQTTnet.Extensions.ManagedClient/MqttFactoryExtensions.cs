using System;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class MqttFactoryExtensions
    {
        public static IManagedMqttClient CreateManagedMqttClient(this IMqttClientFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new ManagedMqttClient(factory.CreateMqttClient(), new MqttNetLogger().CreateChildLogger());
        }

        public static IManagedMqttClient CreateManagedMqttClient(this IMqttClientFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(factory.CreateMqttClient(), logger.CreateChildLogger());
        }
    }
}
