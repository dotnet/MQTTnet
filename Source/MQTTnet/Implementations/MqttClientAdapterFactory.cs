// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Formatter;
using System;
using MQTTnet.Channel;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Implementations
{
    public sealed class MqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(MqttClientOptions options, MqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            ArgumentNullException.ThrowIfNull(options);

            IMqttChannel channel;
            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                    {
                        channel = new MqttTcpChannel(options);
                        break;
                    }

                case MqttClientWebSocketOptions webSocketOptions:
                    {
                        channel = new MqttWebSocketChannel(webSocketOptions);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            var bufferWriter = new MqttBufferWriter(options.WriterBufferSize, options.WriterBufferSizeMax);
            var packetFormatterAdapter = new MqttPacketFormatterAdapter(options.ProtocolVersion, bufferWriter);

            return new MqttChannelAdapter(channel, packetFormatterAdapter, logger)
            {
                AllowPacketFragmentation = options.AllowPacketFragmentation,
                PacketInspector = packetInspector
            };
        }
    }
}
