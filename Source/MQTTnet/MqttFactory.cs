using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet
{
    public class MqttFactory : IMqttFactory
    {
        public MqttFactory() : this(new MqttNetLogger())
        {
        }

        public MqttFactory(IMqttNetLogger logger)
        {
            DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMqttNetLogger DefaultLogger { get; }

        public IMqttClient CreateMqttClient()
        {
            return CreateMqttClient(DefaultLogger);
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttClient(new MqttClientAdapterFactory(), logger);
        }

        public IMqttClient CreateMqttClient(IMqttClientAdapterFactory adapterFactory)
        {
            if (adapterFactory == null) throw new ArgumentNullException(nameof(adapterFactory));

            return new MqttClient(adapterFactory, DefaultLogger);
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory adapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (adapterFactory == null) throw new ArgumentNullException(nameof(adapterFactory));

            return new MqttClient(adapterFactory, logger);
        }

        public IMqttServer CreateMqttServer()
        {
            return CreateMqttServer(DefaultLogger);
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

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> adapters)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            
            return new MqttServer(adapters, DefaultLogger.CreateChildLogger());
        }
    }
}