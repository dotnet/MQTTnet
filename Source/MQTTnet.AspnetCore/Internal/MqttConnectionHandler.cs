// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Options;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

sealed class MqttConnectionHandler : ConnectionHandler
{
    readonly IOptions<MqttServerOptions> _serverOptions;

    public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

    public MqttConnectionHandler(IOptions<MqttServerOptions> serverOptions)
    {
        _serverOptions = serverOptions;
    }

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var clientHandler = ClientHandler;
        if (clientHandler == null)
        {
            // MqttServer has not been started yet.
            connection.Abort();
            return;
        }

        // required for websocket transport to work
        var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
        if (transferFormatFeature != null)
        {
            transferFormatFeature.ActiveFormat = TransferFormat.Binary;
        }

        var options = _serverOptions.Value;
        var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(options.WriterBufferSize, options.WriterBufferSizeMax));
        using var adapter = new AspNetCoreMqttChannelAdapter(formatter, connection);
        await clientHandler(adapter).ConfigureAwait(false);
    }
}