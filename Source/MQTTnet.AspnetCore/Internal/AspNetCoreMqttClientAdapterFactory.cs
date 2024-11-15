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
            ArgumentNullException.ThrowIfNull(nameof(options));
            var bufferWriter = new MqttBufferWriter(options.WriterBufferSize, options.WriterBufferSizeMax);
            var formatter = new MqttPacketFormatterAdapter(options.ProtocolVersion, bufferWriter);
            return new MqttClientChannelAdapter(formatter, options.ChannelOptions);
        }
    }
}
