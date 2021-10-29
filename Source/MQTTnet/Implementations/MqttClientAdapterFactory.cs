using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using System;
using MQTTnet.Channel;
using MQTTnet.Client;

namespace MQTTnet.Implementations
{
    public sealed class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetLogger logger)
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
            return new MqttChannelAdapter(channel, packetFormatterAdapter, options.PacketInspector, logger);
        }
    }
}
