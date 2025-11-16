// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public sealed class MqttHostedServer : BackgroundService
{
    readonly MqttServerFactory _mqttFactory;
    public MqttHostedServer(
        MqttServerFactory mqttFactory,
        MqttServerOptions options,
        IEnumerable<IMqttServerAdapter> adapters,
        IMqttNetLogger logger
        )
    {
        MqttServer = new(options, adapters, logger);
        _mqttFactory = mqttFactory ?? throw new ArgumentNullException(nameof(mqttFactory));
    }

    public MqttServer MqttServer { get; }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => MqttServer.StartAsync();
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await MqttServer.StopAsync(_mqttFactory.CreateMqttServerStopOptionsBuilder().Build());
        await base.StopAsync(cancellationToken);
    }
}
