using System;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;

namespace MQTTnet.Implementations
{
    public class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientChannelOptions options, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options)
            {
                case MqttClientTcpOptions tcpOptions:
                    return new MqttChannelAdapter(new MqttTcpChannel(tcpOptions), new MqttPacketSerializer(), logger);
                case MqttClientWebSocketOptions webSocketOptions:
                    return new MqttChannelAdapter(new MqttWebSocketChannel(webSocketOptions), new MqttPacketSerializer(), logger);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
