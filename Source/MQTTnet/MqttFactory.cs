using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;
using MQTTnet.Server;
using System;
using System.Collections.Generic;

namespace MQTTnet
{
    public sealed class MqttFactory : IMqttFactory
    {
        IMqttClientAdapterFactory _clientAdapterFactory = new MqttClientAdapterFactory();

        public MqttFactory() : this(new MqttNetLogger())
        {
        }

        public MqttFactory(IMqttNetLogger logger)
        {
            DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMqttNetLogger DefaultLogger { get; }

        public IMqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory)
        {
            _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));
            return this;
        }

        public ILowLevelMqttClient CreateLowLevelMqttClient()
        {
            return CreateLowLevelMqttClient(DefaultLogger);
        }

        public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new LowLevelMqttClient(_clientAdapterFactory, logger);
        }

        public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new LowLevelMqttClient(_clientAdapterFactory, DefaultLogger);
        }

        public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactoryy)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (clientAdapterFactoryy == null) throw new ArgumentNullException(nameof(clientAdapterFactoryy));

            return new LowLevelMqttClient(_clientAdapterFactory, logger);
        }

        public IMqttClient CreateMqttClient()
        {
            return CreateMqttClient(DefaultLogger);
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttClient(_clientAdapterFactory, logger);
        }

        public IMqttClient CreateMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new MqttClient(clientAdapterFactory, DefaultLogger);
        }

        public IMqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new MqttClient(clientAdapterFactory, logger);
        }

        public IMqttServer CreateMqttServer()
        {
            return CreateMqttServer(DefaultLogger);
        }

        public IMqttServer CreateMqttServer(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return CreateMqttServer(new List<IMqttServerAdapter> { new MqttTcpServerAdapter(logger) }, logger);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> serverAdapters, IMqttNetLogger logger)
        {
            if (serverAdapters == null) throw new ArgumentNullException(nameof(serverAdapters));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttServer(serverAdapters, logger);
        }

        public IMqttServer CreateMqttServer(IEnumerable<IMqttServerAdapter> serverAdapters)
        {
            if (serverAdapters == null) throw new ArgumentNullException(nameof(serverAdapters));

            return new MqttServer(serverAdapters, DefaultLogger);
        }
    }
}