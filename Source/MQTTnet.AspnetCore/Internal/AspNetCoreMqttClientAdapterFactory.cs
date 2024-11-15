// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using System;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(MqttClientOptions options, MqttPacketInspector packetInspector, IMqttNetLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            switch (options.ChannelOptions)
            {
                case MqttClientTcpOptions tcpOptions:
                    {
                        var formatter = new MqttPacketFormatterAdapter(options.ProtocolVersion, new MqttBufferWriter(4096, 65535));
                        return new MqttClientChannelAdapter(formatter, tcpOptions);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
    }
}
