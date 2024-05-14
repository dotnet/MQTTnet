// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using MQTTnet.Server.Adapter;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttHostedServer : MqttServer, IHostedService
    {
        readonly MqttServerFactory _mqttServerFactory;

        public MqttHostedServer(MqttServerFactory mqttServerFactory, MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger) : base(
            options,
            adapters,
            logger)
        {
            _mqttServerFactory = mqttServerFactory ?? throw new ArgumentNullException(nameof(mqttServerFactory));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // The yield makes sure that the hosted service is considered up and running.
            await Task.Yield();

            _ = StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync(_mqttServerFactory.CreateMqttServerStopOptionsBuilder().Build());
        }
    }
}