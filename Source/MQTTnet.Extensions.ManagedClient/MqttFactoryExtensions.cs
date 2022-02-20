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
        public static ManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, MqttClient mqttClient = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (mqttClient == null)
            {
                return new ManagedMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);    
            }
            
            return new ManagedMqttClient(mqttClient, factory.DefaultLogger);
        }
        
        public static ManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(factory.CreateMqttClient(logger), logger);
        }
    }
}