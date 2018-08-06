using System;
using System.Net;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;

namespace MQTTnet.AspNetCore.Client
{
    public class MqttClientConnectionContextFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions tcpOptions:
                    {
                        var endpoint = new DnsEndPoint(tcpOptions.Server, tcpOptions.GetPort());
                        var tcpConnection = new TcpConnection(endpoint);
                        return new MqttConnectionContext(new MqttPacketSerializerAdapter(options.ProtocolVersion), tcpConnection);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
