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

namespace MQTTnet.AspNetCore
{
    public sealed class MqttHostedServer : MqttServer, IHostedService
    {
        readonly MqttFactory _mqttFactory;
#if NETCOREAPP3_1_OR_GREATER
        readonly IHostApplicationLifetime _hostApplicationLifetime;
        public MqttHostedServer(IHostApplicationLifetime hostApplicationLifetime, MqttFactory mqttFactory,
            MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger) : base(
            options,
            adapters,
            logger)
        {
            _mqttFactory = mqttFactory ?? throw new ArgumentNullException(nameof(mqttFactory));
            _hostApplicationLifetime = hostApplicationLifetime;
        }
#else
        public MqttHostedServer(MqttFactory mqttFactory,
            MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger) : base(
            options,
            adapters,
            logger)
        {
            _mqttFactory = mqttFactory ?? throw new ArgumentNullException(nameof(mqttFactory));
        }
#endif


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // The yield makes sure that the hosted service is considered up and running.
            await Task.Yield();
#if NETCOREAPP3_1_OR_GREATER
            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
#else
            _ = StartAsync();
#endif

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return StopAsync(_mqttFactory.CreateMqttServerStopOptionsBuilder().Build());
        }

#if NETCOREAPP3_1_OR_GREATER
        private void OnStarted()
        {
            _ = StartAsync();
        }
#endif
    }
}