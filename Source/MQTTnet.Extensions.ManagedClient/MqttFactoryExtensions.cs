﻿using System;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class MqttFactoryExtensions
    {
        public static IManagedMqttClient CreateManagedMqttClient(this MqttFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new ManagedMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);
        }

        public static IManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(factory.CreateMqttClient(logger), logger);
        }
    }
}