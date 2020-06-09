using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using System;

namespace MQTTnet.Implementations
{
    public class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttNetLogger _logger;

        public MqttClientAdapterFactory(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                    {
                        return new MqttChannelAdapter(new MqttTcpChannel(options), new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttPacketWriter()), _logger);
                    }

                case MqttClientWebSocketOptions webSocketOptions:
                    {
                        return new MqttChannelAdapter(new MqttWebSocketChannel(webSocketOptions), new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttPacketWriter()), _logger);
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
