using System;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;

namespace MQTTnet.Implementations
{
    public class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            
            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                    {
                        return new MqttChannelAdapter(new MqttTcpChannel(options), new MqttPacketSerializerAdapter(options.ProtocolVersion), logger);
                    }

                case MqttClientWebSocketOptions webSocketOptions:
                    {
                        return new MqttChannelAdapter(new MqttWebSocketChannel(webSocketOptions), new MqttPacketSerializerAdapter(options.ProtocolVersion), logger);
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
