using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
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

        public IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory)
        {
            if (adapterFactory == null) throw new ArgumentNullException(nameof(adapterFactory));

            return new MqttClient(adapterFactory, new MqttNetLogger());
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (adapterFactory == null) throw new ArgumentNullException(nameof(adapterFactory));

            return new MqttClient(adapterFactory, logger);
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