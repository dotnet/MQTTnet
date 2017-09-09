using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Serializer;
using MQTTnet.Implementations;

namespace MQTTnet
{
    public class MqttClientFactory
    {
        public IMqttClient CreateMqttClient(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttClient(options, new MqttChannelCommunicationAdapter(GetMqttCommunicationChannel(options), new MqttPacketSerializer()));
        }

        private static IMqttCommunicationChannel GetMqttCommunicationChannel(MqttClientOptions options)
        {
            switch (options.ConnectionType)
            {
                case MqttConnectionType.Tcp:
                case MqttConnectionType.Tls:
                    return new MqttTcpChannel();
                case MqttConnectionType.Ws:
                case MqttConnectionType.Wss:
                    return new MqttWebSocketChannel();

                default:
                    throw new NotSupportedException();
            }
        }
    }
}