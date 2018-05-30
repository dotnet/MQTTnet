using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.ManagedClient;
using MQTTnet.Server;

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
            return new ManagedMqttClient(CreateMqttClient(), new MqttNetLogger().CreateChildLogger());
        }

        public IManagedMqttClient CreateManagedMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new ManagedMqttClient(CreateMqttClient(), logger.CreateChildLogger());
        }

        public IMqttServer CreateMqttServer()
        {
            var logger = new MqttNetLogger();
            return CreateMqttServer(logger);
        }

        public IMqttServer CreateMqttServer(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return CreateMqttServer(new List<IMqttServerAdapter> { new MqttTcpServerAdapter(logger.CreateChildLogger()) }, logger);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttServer(adapters, logger.CreateChildLogger());
        }
    }
}