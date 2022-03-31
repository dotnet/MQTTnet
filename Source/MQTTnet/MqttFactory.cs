// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Implementations;
using MQTTnet.LowLevelClient;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Diagnostics;
using MqttClient = MQTTnet.Client.MqttClient;

namespace MQTTnet
{
    public sealed class MqttFactory
    {
        IMqttClientAdapterFactory _clientAdapterFactory;

        public MqttFactory() : this(new MqttNetNullLogger())
        {
        }

        public MqttFactory(IMqttNetLogger logger)
        {
            DefaultLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _clientAdapterFactory = new MqttClientAdapterFactory();
        }

        public IMqttNetLogger DefaultLogger { get; }

        public IList<Func<MqttFactory, IMqttServerAdapter>> DefaultServerAdapters { get; } = new List<Func<MqttFactory, IMqttServerAdapter>>
        {
            factory => new MqttTcpServerAdapter()
        };

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        public MqttFactory UseClientAdapterFactory(IMqttClientAdapterFactory clientAdapterFactory)
        {
            _clientAdapterFactory = clientAdapterFactory ?? throw new ArgumentNullException(nameof(clientAdapterFactory));
            return this;
        }

        public LowLevelMqttClient CreateLowLevelMqttClient()
        {
            return CreateLowLevelMqttClient(DefaultLogger);
        }

        public LowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new LowLevelMqttClient(_clientAdapterFactory, logger);
        }

        public LowLevelMqttClient CreateLowLevelMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new LowLevelMqttClient(_clientAdapterFactory, DefaultLogger);
        }

        public LowLevelMqttClient CreateLowLevelMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new LowLevelMqttClient(_clientAdapterFactory, logger);
        }

        public MqttClient CreateMqttClient()
        {
            return CreateMqttClient(DefaultLogger);
        }

        public MqttClient CreateMqttClient(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttClient(_clientAdapterFactory, logger);
        }

        public MqttClient CreateMqttClient(IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new MqttClient(clientAdapterFactory, DefaultLogger);
        }

        public MqttClient CreateMqttClient(IMqttNetLogger logger, IMqttClientAdapterFactory clientAdapterFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (clientAdapterFactory == null) throw new ArgumentNullException(nameof(clientAdapterFactory));

            return new MqttClient(clientAdapterFactory, logger);
        }

        public MqttServer CreateMqttServer(MqttServerOptions options)
        {
            return CreateMqttServer(options, DefaultLogger);
        }

        public MqttServer CreateMqttServer(MqttServerOptions options, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var serverAdapters = DefaultServerAdapters.Select(a => a.Invoke(this));
            return CreateMqttServer(options, serverAdapters, logger);
        }

        public MqttServer CreateMqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> serverAdapters, IMqttNetLogger logger)
        {
            if (serverAdapters == null) throw new ArgumentNullException(nameof(serverAdapters));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new MqttServer(options, serverAdapters, logger);
        }

        public MqttServer CreateMqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> serverAdapters)
        {
            if (serverAdapters == null) throw new ArgumentNullException(nameof(serverAdapters));

            return new MqttServer(options, serverAdapters, DefaultLogger);
        }
        
        public MqttServerOptionsBuilder CreateServerOptionsBuilder()
        {
            return new MqttServerOptionsBuilder();
        }
       
        public MqttClientOptionsBuilder CreateClientOptionsBuilder()
        {
            return new MqttClientOptionsBuilder();
        }

        public MqttClientDisconnectOptionsBuilder CreateClientDisconnectOptionsBuilder()
        {
            return new MqttClientDisconnectOptionsBuilder();
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