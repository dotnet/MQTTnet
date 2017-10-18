using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Implementations
{
    public class MqttCommunicationAdapterFactory : IMqttCommunicationAdapterFactory
    {
        public IMqttCommunicationAdapter CreateMqttCommunicationAdapter(MqttClientQueuedOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options is MqttClientTcpOptions tcpOptions)
            {
                return new MqttChannelCommunicationAdapter(new MqttTcpChannel(tcpOptions), new MqttPacketSerializer { ProtocolVersion = options.ProtocolVersion });
            }

            if (options is MqttClientWebSocketOptions webSocketOptions)
            {
                return new MqttChannelCommunicationAdapter(new MqttWebSocketChannel(webSocketOptions), new MqttPacketSerializer { ProtocolVersion = options.ProtocolVersion });
            }

            throw new NotSupportedException();
        }
    }
}