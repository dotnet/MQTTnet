﻿using System;
using System.Collections.Generic;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Implementations;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Core.Server;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet
{
    public class MqttFactory : IMqttClientFactory, IMqttServerFactory
    {
        public IMqttClient CreateMqttClient()
        {
            return CreateMqttClient(new MqttNetLogger());
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttClient(new MqttClientAdapterFactory(), logger);
        }

        public IManagedMqttClient CreateManagedMqttClient()
        {
            return new ManagedMqttClient(CreateMqttClient(), new MqttNetLogger());
        }

        public IManagedMqttClient CreateManagedMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(CreateMqttClient(), logger);
        }

        public IMqttServer CreateMqttServer()
        {
            var logger = new MqttNetLogger();
            return CreateMqttServer(new List<IMqttServerAdapter> { new MqttServerAdapter(logger) }, logger);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttServer(adapters, logger);
        }
    }
}