// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public sealed class MqttHostedServer : MqttServer, IHostedService
{
    readonly IHostApplicationLifetime _hostApplicationLifetime;
    readonly MqttServerFactory _mqttFactory;

    public MqttHostedServer(
        IHostApplicationLifetime hostApplicationLifetime,
        MqttServerFactory mqttFactory,
        MqttServerOptions options,
        IEnumerable<IMqttServerAdapter> adapters,
        IMqttNetLogger logger) : base(options, adapters, logger)
    {
        _mqttFactory = mqttFactory ?? throw new ArgumentNullException(nameof(mqttFactory));
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // The yield makes sure that the hosted service is considered up and running.
        await Task.Yield();

        _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAsync(_mqttFactory.CreateMqttServerStopOptionsBuilder().Build());
    }

    void OnStarted()
    {
        _ = StartAsync();
    }
}