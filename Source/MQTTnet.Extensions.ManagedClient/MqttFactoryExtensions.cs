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
        public static IManagedMqttClient CreateManagedMqttClient(this MqttClientFactory clientFactory, IMqttClient mqttClient = null)
        {
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            if (mqttClient == null)
            {
                return new ManagedMqttClient(clientFactory.CreateMqttClient(), clientFactory.DefaultLogger);
            }

            return new ManagedMqttClient(mqttClient, clientFactory.DefaultLogger);
        }

        public static IManagedMqttClient CreateManagedMqttClient(this MqttClientFactory clientFactory, IMqttNetLogger logger)
        {
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return new ManagedMqttClient(clientFactory.CreateMqttClient(logger), logger);
        }

        public static ManagedMqttClientOptionsBuilder CreateManagedMqttClientOptionsBuilder(this MqttClientFactory _)
        {
            return new ManagedMqttClientOptionsBuilder();
        }
    }
}