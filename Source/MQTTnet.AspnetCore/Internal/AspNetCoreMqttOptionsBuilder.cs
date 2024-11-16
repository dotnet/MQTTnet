// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    sealed class AspNetCoreMqttOptionsBuilder
    {
        private readonly MqttServerOptionsBuilder _serverOptionsBuilder;
        private readonly MqttServerStopOptionsBuilder _stopOptionsBuilder;

        public AspNetCoreMqttOptionsBuilder(
            IOptions<MqttServerOptionsBuilder> serverOptionsBuilderOptions,
            IOptions<MqttServerStopOptionsBuilder> stopOptionsBuilderOptions)
        {
            _serverOptionsBuilder = serverOptionsBuilderOptions.Value;
            _stopOptionsBuilder = stopOptionsBuilderOptions.Value;
        }

        public MqttServerOptions BuildServerOptions()
        {
            return _serverOptionsBuilder.Build();
        }

        public MqttServerStopOptions BuildServerStopOptions()
        {
            return _stopOptionsBuilder.Build();
        }
    }
}
