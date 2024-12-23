// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    private readonly MqttServerStopOptions _stopOptions;
    private readonly IEnumerable<IMqttServerAdapter> _adapters;

    public AspNetCoreMqttServer(
        MqttConnectionHandler connectionHandler,
        MqttServerOptions serverOptions,
        MqttServerStopOptions stopOptions,
        IEnumerable<IMqttServerAdapter> adapters,
        IMqttNetLogger logger) : base(serverOptions, adapters, logger)
    {
        _connectionHandler = connectionHandler;
        _stopOptions = stopOptions;
        _adapters = adapters;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_connectionHandler.ListenFlag &&
            !_connectionHandler.UseFlag &&
            !_connectionHandler.MapFlag &&
            _adapters.All(item => item.GetType() == typeof(AspNetCoreMqttServerAdapter)))
        {
            throw new MqttConfigurationException("ListenMqtt() or UseMqtt() or MapMqtt() must be called in at least one place");
        }

        return base.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(_stopOptions);
    }
}