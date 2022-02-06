// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using MQTTnet.Adapter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;

namespace MQTTnet.AspNetCore
{
    public sealed class MqttConnectionHandler : ConnectionHandler, IMqttServerAdapter
    {
        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            // required for websocket transport to work
            var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
            if (transferFormatFeature != null)
            {
                transferFormatFeature.ActiveFormat = TransferFormat.Binary;
            }

            var writer = new SpanBasedMqttPacketWriter();
            var formatter = new MqttPacketFormatterAdapter(writer);
            using (var adapter = new MqttConnectionContext(formatter, connection))
            {
                var clientHandler = ClientHandler;
                if (clientHandler != null)
                {
                    await clientHandler(adapter).ConfigureAwait(false);
                }
            }
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
}
