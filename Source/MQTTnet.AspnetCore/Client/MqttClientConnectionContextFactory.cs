// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Formatter;
using System;
using System.Net;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.AspNetCore.Client
{
    public sealed class MqttClientConnectionContextFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(MqttClientOptions options, IMqttPacketInspectorHandler packetInspectorHandler, IMqttNetLogger logger)
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
