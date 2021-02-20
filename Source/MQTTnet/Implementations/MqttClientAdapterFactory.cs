using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using System;
using MQTTnet.Channel;

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

            IMqttChannel channel;
            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                    {
                        channel = new MqttTcpChannel(options);
                        break;
                    }

                case MqttClientWebSocketOptions webSocketOptions:
                    {
                        channel = new MqttWebSocketChannel(webSocketOptions);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            var packetFormatterAdapter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttPacketWriter());
            return new MqttChannelAdapter(channel, packetFormatterAdapter, options.PacketInspector, _logger);
        }
    }
}
