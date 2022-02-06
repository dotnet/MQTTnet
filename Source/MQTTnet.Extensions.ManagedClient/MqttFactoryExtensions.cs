// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.ManagedClient
{
    public static class MqttFactoryExtensions
    {
        public static ManagedMqttClient CreateManagedMqttClient(this MqttFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new ManagedMqttClient(factory.CreateMqttClient(), factory.DefaultLogger);
        }

        public static ManagedMqttClient CreateManagedMqttClient(this MqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(factory.CreateMqttClient(logger), logger);
        }
    }
}