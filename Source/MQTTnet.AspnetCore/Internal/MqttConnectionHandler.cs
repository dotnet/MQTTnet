// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Options;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class MqttConnectionHandler : ConnectionHandler
{
    readonly IMqttNetLogger _logger;
    readonly MqttServerOptions _serverOptions;

    public Func<IMqttChannelAdapter, Task>? ClientHandler { get; set; }

    public MqttConnectionHandler(
        IMqttNetLogger logger,
        IOptions<MqttServerOptionsBuilder> serverOptions)
    {
        _logger = logger;
        _serverOptions = serverOptions.Value.Build();
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var clientHandler = ClientHandler;
        if (clientHandler == null)
        {
            connection.Abort();
            _logger.Publish(MqttNetLogLevel.Warning, nameof(MqttConnectionHandler), $"{nameof(MqttServer)} has not been started yet.", null, null);
            return;
        }

        // required for websocket transport to work
        var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
        if (transferFormatFeature != null)
        {
            transferFormatFeature.ActiveFormat = TransferFormat.Binary;
        }

        var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(_serverOptions.WriterBufferSize, _serverOptions.WriterBufferSizeMax));
        using var adapter = new MqttServerChannelAdapter(formatter, connection);
        await clientHandler(adapter).ConfigureAwait(false);
    }
}