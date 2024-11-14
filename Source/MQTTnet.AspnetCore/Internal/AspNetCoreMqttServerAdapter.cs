// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class AspNetCoreMqttServerAdapter : IMqttServerAdapter
{
    readonly MqttConnectionHandler _connectionHandler;

    public Func<IMqttChannelAdapter, Task>? ClientHandler
    {
        get => _connectionHandler.ClientHandler;
        set => _connectionHandler.ClientHandler = value;
    }

    public AspNetCoreMqttServerAdapter(MqttConnectionHandler connectionHandler)
    {
        _connectionHandler = connectionHandler;
    }

    public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}