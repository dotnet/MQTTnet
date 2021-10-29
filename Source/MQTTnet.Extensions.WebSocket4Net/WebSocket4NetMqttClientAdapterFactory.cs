using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using System;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public sealed class WebSocket4NetMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetLogger logger)
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
                        logger);
                }

                case MqttClientWebSocketOptions webSocketOptions:
                {
                    return new MqttChannelAdapter(
                        new WebSocket4NetMqttChannel(options, webSocketOptions),
                        new MqttPacketFormatterAdapter(options.ProtocolVersion), 
                        options.PacketInspector,
                        logger);
                }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
