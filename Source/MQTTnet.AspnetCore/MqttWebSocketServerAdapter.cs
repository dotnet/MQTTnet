// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public sealed class MqttWebSocketServerAdapter : IMqttServerAdapter
{
    IMqttNetLogger _logger = MqttNetNullLogger.Instance;

    public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

    public void Dispose()
    {
    }

    public async Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(webSocket);

        var remoteAddress = httpContext.Connection.RemoteIpAddress;
        var remoteEndPoint = remoteAddress == null ? null : new IPEndPoint(remoteAddress, httpContext.Connection.RemotePort);

        var clientCertificate = await httpContext.Connection.GetClientCertificateAsync().ConfigureAwait(false);
        try
        {
            var isSecureConnection = clientCertificate != null;

            var clientHandler = ClientHandler;
            if (clientHandler != null)
            {
                var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535));
                var channel = new MqttWebSocketChannel(webSocket, remoteEndPoint, isSecureConnection, clientCertificate);

                using var channelAdapter = new MqttChannelAdapter(channel, formatter, _logger);
                await clientHandler(channelAdapter).ConfigureAwait(false);
            }
        }
        finally
        {
            clientCertificate?.Dispose();
        }
    }

    public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}