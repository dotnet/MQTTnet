// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        private readonly IConnectionFactory connectionFactory;

        public AspNetCoreMqttClientAdapterFactory(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public async ValueTask<IMqttChannelAdapter> CreateClientAdapterAsync(MqttClientOptions options, MqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions tcpOptions:
                    {
                        var endPoint = await CreateIPEndPointAsync(tcpOptions.RemoteEndpoint);
                        var tcpConnection = await connectionFactory.ConnectAsync(endPoint);
                        var formatter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttBufferWriter(4096, 65535));
                        return new AspNetCoreMqttChannelAdapter(formatter, tcpConnection);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private static async ValueTask<IPEndPoint> CreateIPEndPointAsync(EndPoint endpoint)
        {
            if (endpoint is IPEndPoint ipEndPoint)
            {
                return ipEndPoint;
            }

            if (endpoint is DnsEndPoint dnsEndPoint)
            {
                var hostEntry = await Dns.GetHostEntryAsync(dnsEndPoint.Host);
                var address = hostEntry.AddressList.OrderBy(item => item.AddressFamily).FirstOrDefault();
                return address == null
                    ? throw new SocketException((int)SocketError.HostNotFound)
                    : new IPEndPoint(address, dnsEndPoint.Port);
            }

            throw new NotSupportedException("Only supports IPEndPoint or DnsEndPoint for now.");
        }
    }
}
