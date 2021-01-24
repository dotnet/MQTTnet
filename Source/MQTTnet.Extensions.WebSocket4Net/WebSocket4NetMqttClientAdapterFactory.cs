using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using System;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public sealed class WebSocket4NetMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        readonly IMqttNetLogger _logger;

        public WebSocket4NetMqttClientAdapterFactory(IMqttNetLogger logger)
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
                    return new MqttChannelAdapter(
                        new MqttTcpChannel(options),
                        new MqttPacketFormatterAdapter(options.ProtocolVersion),
                        options.PacketInspector,
                        _logger);
                }

                case MqttClientWebSocketOptions webSocketOptions:
                {
                    return new MqttChannelAdapter(
                        new WebSocket4NetMqttChannel(options, webSocketOptions),
                        new MqttPacketFormatterAdapter(options.ProtocolVersion), 
                        options.PacketInspector,
                        _logger);
                }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
