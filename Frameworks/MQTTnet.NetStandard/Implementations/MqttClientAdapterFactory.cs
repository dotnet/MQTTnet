using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Serializer;

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
