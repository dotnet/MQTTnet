// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Server;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class AspNetCoreMqttServer : MqttServer
{
    private readonly MqttConnectionHandler _connectionHandler;
    private readonly IOptions<MqttServerStopOptionsBuilder> _stopOptions;
    private readonly IEnumerable<IMqttServerAdapter> _adapters;

    public AspNetCoreMqttServer(
        MqttConnectionHandler connectionHandler,
        IOptions<MqttServerOptionsBuilder> serverOptions,
        IOptions<MqttServerStopOptionsBuilder> stopOptions,
        IEnumerable<IMqttServerAdapter> adapters,
        IMqttNetLogger logger) : base(serverOptions.Value.Build(), adapters, logger)
    {
        _connectionHandler = connectionHandler;
        _stopOptions = stopOptions;
        _adapters = adapters;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_connectionHandler.UseFlag &&
            !_connectionHandler.MapFlag &&
            _adapters.All(item => item.GetType() == typeof(AspNetCoreMqttServerAdapter)))
        {
            throw new MqttConfigurationException("UseMqtt() or MapMqtt() must be called in at least one place");
        }

        return base.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(_stopOptions.Value.Build());
    }
}