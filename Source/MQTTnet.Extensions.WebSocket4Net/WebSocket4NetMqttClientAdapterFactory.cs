﻿using System;
using MQTTnet.Adapter;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public class WebSocket4NetMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions _:
                {
                    return new MqttChannelAdapter(new MqttTcpChannel(options), new MqttPacketFormatterAdapter(options.ProtocolVersion), logger);
                }

                case MqttClientWebSocketOptions webSocketOptions:
                {
                    return new MqttChannelAdapter(new WebSocket4NetMqttChannel(options, webSocketOptions), new MqttPacketFormatterAdapter(options.ProtocolVersion), logger);
                }

                default:
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
