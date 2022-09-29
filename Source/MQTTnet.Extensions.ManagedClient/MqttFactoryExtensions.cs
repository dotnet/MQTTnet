// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class MqttFactoryExtensions
    {
        public static IManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttClient mqttClient, IMqttNetLogger logger)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (mqttClient == null)
            {
                return new NewManagedMqttClient(factory.CreateMqttClient(), logger);
            }

            return new NewManagedMqttClient(mqttClient, logger);
        }

        public static IManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttClient mqttClient = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (mqttClient == null)
            {
                return new NewManagedMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);
            }

            return new NewManagedMqttClient(mqttClient, factory.DefaultLogger);
        }

        public static IManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new NewManagedMqttClient(factory.CreateMqttClient(logger), logger);
        }

        public static ManagedMqttClientOptionsBuilder CreateManagedMqttClientOptionsBuilder(this MqttFactory factory)
        {
            return new ManagedMqttClientOptionsBuilder();
        }
    }
}