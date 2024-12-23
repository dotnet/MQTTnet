// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

/// <summary>
/// Middleware that connection using the specified MQTT protocols
/// </summary>
sealed class MqttConnectionMiddleware
{
    private static readonly byte[] _mqtt = "MQTT"u8.ToArray();
    private static readonly byte[] _mqisdp = "MQIsdp"u8.ToArray();
    private readonly MqttConnectionHandler _connectionHandler;

    public MqttConnectionMiddleware(MqttConnectionHandler connectionHandler)
    {
        _connectionHandler = connectionHandler;
    }

    public async Task InvokeAsync(
        ConnectionDelegate next,
        ConnectionContext connection,
        MqttProtocols protocols,
        Func<IMqttChannelAdapter, bool>? allowPacketFragmentationSelector)
    {
        if (allowPacketFragmentationSelector != null)
        {
            connection.Features.Set(new PacketFragmentationFeature(allowPacketFragmentationSelector));
        }

        if (protocols == MqttProtocols.MqttAndWebSocket)
        {
            var input = connection.Transport.Input;
            var readResult = await input.ReadAsync();
            var isMqtt = IsMqttRequest(readResult.Buffer);
            input.AdvanceTo(readResult.Buffer.Start);

            protocols = isMqtt ? MqttProtocols.Mqtt : MqttProtocols.WebSocket;
        }

        if (protocols == MqttProtocols.Mqtt)
        {
            await _connectionHandler.OnConnectedAsync(connection).ConfigureAwait(false);
        }
        else if (protocols == MqttProtocols.WebSocket)
        {
            await next(connection).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException(protocols.ToString());
        }
    }

    public static bool IsMqttRequest(ReadOnlySequence<byte> buffer)
    {
        if (!buffer.IsEmpty)
        {
            var span = buffer.FirstSpan;
            if (span.Length > 4)
            {
                var protocol = span[4..];
                return protocol.StartsWith(_mqtt) || protocol.StartsWith(_mqisdp);
            }
        }

        return false;
    }
}