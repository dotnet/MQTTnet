﻿using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Formatter;
using System;
using System.Net;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.AspNetCore.Client
{
    public class MqttClientConnectionContextFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions tcpOptions:
                    {
                        var endpoint = new DnsEndPoint(tcpOptions.Server, tcpOptions.GetPort());
                        var tcpConnection = new TcpConnection(endpoint);

                        var writer = new SpanBasedMqttPacketWriter();
                        var formatter = new MqttPacketFormatterAdapter(options.ProtocolVersion, writer);
                        return new MqttConnectionContext(formatter, tcpConnection);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
