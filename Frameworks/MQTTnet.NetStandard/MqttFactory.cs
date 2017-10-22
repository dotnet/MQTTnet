using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Implementations;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Core.Server;
using System.Linq;
using MQTTnet.Core.Channel;

namespace MQTTnet
{
    public class MqttFactory : IMqttCommunicationAdapterFactory, IMqttClientSesssionFactory, IMqttClientFactory, IMqttServerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        private static IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddMqttClient()
                .AddMqttServer()
                .AddLogging()
                .BuildServiceProvider();
        }

        public MqttFactory()
            : this(BuildServiceProvider())
        {
        }

        public MqttFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMqttCommunicationAdapter CreateClientMqttCommunicationAdapter(IMqttClientOptions options)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<MqttChannelCommunicationAdapter>>();
            return new MqttChannelCommunicationAdapter(CreateMqttCommunicationChannel(options), CreateSerializer(options.ProtocolVersion), logger);
        }

        public IMqttCommunicationAdapter CreateServerMqttCommunicationAdapter(IMqttCommunicationChannel channel)
        {
            var serializer = _serviceProvider.GetRequiredService<IMqttPacketSerializer>();
            var logger = _serviceProvider.GetRequiredService<ILogger<MqttChannelCommunicationAdapter>>();
            return new MqttChannelCommunicationAdapter(channel, serializer, logger);
        }

        public IMqttCommunicationChannel CreateMqttCommunicationChannel(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options)
            {
                case MqttClientTcpOptions tcpOptions:
                    return CreateTcpChannel(tcpOptions);
                case MqttClientWebSocketOptions webSocketOptions:
                    return CreateWebSocketChannel(webSocketOptions);
                default:
                    throw new NotSupportedException();
            }
        }        

        public MqttTcpChannel CreateTcpChannel(MqttClientTcpOptions tcpOptions)
        {
            return new MqttTcpChannel(tcpOptions);
        }

        public MqttWebSocketChannel CreateWebSocketChannel(MqttClientWebSocketOptions webSocketOptions)
        {
            return new MqttWebSocketChannel(webSocketOptions);
        }

        public MqttPacketSerializer CreateSerializer(MqttProtocolVersion protocolVersion)
        {
            return new MqttPacketSerializer()
            {
                ProtocolVersion = protocolVersion
            };
        }

        public MqttClientSession CreateClientSession(string sessionId, MqttClientSessionsManager mqttClientSessionsManager)
        {
            return new MqttClientSession(sessionId, mqttClientSessionsManager, _serviceProvider.GetRequiredService<ILogger<MqttClientSession>>(), _serviceProvider.GetRequiredService<ILogger<MqttClientPendingMessagesQueue>>());
        }

        public IMqttClient CreateMqttClient()
        {
            return _serviceProvider.GetRequiredService<IMqttClient>();
        }

        public ManagedMqttClient CreateManagedMqttClient()
        {
            return _serviceProvider.GetRequiredService<ManagedMqttClient>();
        }

        public IMqttServer CreateMqttServer()
        {
            return _serviceProvider.GetRequiredService<IMqttServer>();
        }
    }
}