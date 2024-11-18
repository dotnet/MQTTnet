// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

/// <summary>
/// Middleware that allows connections to be either HTTP or MQTT
/// </summary>
sealed class MqttConnectionMiddleware
{
    private static readonly byte[] _mqtt = "MQTT"u8.ToArray();
    private static readonly byte[] _MQIsdp = "MQIsdp"u8.ToArray();
    private readonly MqttConnectionHandler _connectionHandler;

    public MqttConnectionMiddleware(MqttConnectionHandler connectionHandler)
    { 
        _connectionHandler = connectionHandler;
    }

    public async Task InvokeAsync(ConnectionDelegate next, ConnectionContext connection)
    {
        var input = connection.Transport.Input;
        var readResult = await input.ReadAsync();
        var isMqtt = IsMqttRequest(readResult);
        input.AdvanceTo(readResult.Buffer.Start);

        if (isMqtt)
        {
            await _connectionHandler.OnConnectedAsync(connection);
        }
        else
        {
            await next(connection);
        }
    }

    private static bool IsMqttRequest(ReadResult readResult)
    {
        var span = readResult.Buffer.FirstSpan;
        if (span.Length > 4)
        {
            span = span[4..];
            return span.StartsWith(_mqtt) || span.StartsWith(_MQIsdp);
        }

        return false;
    }
}