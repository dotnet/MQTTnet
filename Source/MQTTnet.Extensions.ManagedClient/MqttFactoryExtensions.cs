using System;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class MqttFactoryExtensions
    {
        public static IManagedMqttClient CreateManagedMqttClient(this IMqttFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new ManagedMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);
        }

        public static IManagedMqttClient CreateManagedMqttClient(this IMqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(factory.CreateMqttClient(logger), logger);
        }
    }
}