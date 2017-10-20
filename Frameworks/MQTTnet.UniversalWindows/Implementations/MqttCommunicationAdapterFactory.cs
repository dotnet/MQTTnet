using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Implementations
{
    public class MqttCommunicationAdapterFactory : IMqttCommunicationAdapterFactory
    {
        public IMqttCommunicationAdapter CreateMqttCommunicationAdapter(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options is MqttClientTcpOptions tcpOptions)
            {
                var trace = new MqttNetTrace();
                return new MqttChannelCommunicationAdapter(new MqttTcpChannel(tcpOptions), new MqttPacketSerializer { ProtocolVersion = options.ProtocolVersion }, trace);
            }

            if (options is MqttClientWebSocketOptions webSocketOptions)
            {
                var trace = new MqttNetTrace();
                return new MqttChannelCommunicationAdapter(new MqttWebSocketChannel(webSocketOptions), new MqttPacketSerializer { ProtocolVersion = options.ProtocolVersion }, trace);
            }

            throw new NotSupportedException();
        }
    }
}