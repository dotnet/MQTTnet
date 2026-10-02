// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public sealed class MqttConnectionHandler : ConnectionHandler, IMqttServerAdapter
{
    MqttServerOptions _serverOptions;

    public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

    public void Dispose()
    {
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var serverOptions = _serverOptions;
        var clientHandler = ClientHandler;
        if (serverOptions == null || clientHandler == null)
        {
            connection.Abort();
            return;
        }

        // required for websocket transport to work
        var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
        if (transferFormatFeature != null)
        {
            transferFormatFeature.ActiveFormat = TransferFormat.Binary;
        }

        var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(serverOptions.WriterBufferSize, serverOptions.WriterBufferSizeMax));
        using var adapter = new MqttConnectionContext(formatter, connection);
        await clientHandler(adapter).ConfigureAwait(false);
    }

    public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _serverOptions = options;

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}
