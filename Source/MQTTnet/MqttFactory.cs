using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet
{
    public sealed class MqttFactory : IMqttFactory
    {
        IMqttClientAdapterFactory _clientAdapterFactory;

        public MqttFactory() : this(new MqttNetNullLogger())
        {
        }

        public MqttFactory(IMqttNetLogger logger)
        {
            DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientAdapterFactory = new MqttClientAdapterFactory(logger);
        }

        public IMqttNetLogger DefaultLogger { get; }

        public IList<Func<IMqttFactory, IMqttServerAdapter>> DefaultServerAdapters { get; } = new List<Func<IMqttFactory, IMqttServerAdapter>>
        {
            factory => new MqttTcpServerAdapter(factory.DefaultLogger)
        };

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

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

        public ILowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

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

            var serverAdapters = DefaultServerAdapters.Select(a => a.Invoke(this));
            return CreateMqttServer(serverAdapters, logger);
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
       
        public MqttClientOptionsBuilder CreateClientOptionsBuilder()
        {
            return new MqttClientOptionsBuilder();
        }
        
        public MqttServerOptionsBuilder CreateServerOptionsBuilder()
        {
            return new MqttServerOptionsBuilder();
        }
        
        public MqttClientSubscribeOptionsBuilder CreateSubscribeOptionsBuilder()
        {
            return new MqttClientSubscribeOptionsBuilder();
        }
        
        public MqttClientUnsubscribeOptionsBuilder CreateUnsubscribeOptionsBuilder()
        {
            return new MqttClientUnsubscribeOptionsBuilder();
        }
        
        public MqttTopicFilterBuilder CreateTopicFilterBuilder()
        {
            return new MqttTopicFilterBuilder();
        }

        public MqttApplicationMessageBuilder CreateApplicationMessageBuilder()
        {
            return new MqttApplicationMessageBuilder();
        }
    }
}