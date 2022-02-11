// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using System;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public sealed class WebSocket4NetMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(MqttClientOptions options, IMqttPacketInspectorHandler packetInspectorHandler, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                {
                    return new MqttChannelAdapter(
                        new MqttTcpChannel(options),
                        new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttBufferWriter(4096, 65535)),
                        packetInspectorHandler,
                        logger);
                }

                case MqttClientWebSocketOptions webSocketOptions:
                {
                    return new MqttChannelAdapter(
                        new WebSocket4NetMqttChannel(options, webSocketOptions),
                        new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttBufferWriter(4068, 65535)), 
                        packetInspectorHandler,
                        logger);
                }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
