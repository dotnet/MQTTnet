// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public sealed class ConnectionHandlerMockup : IMqttServerAdapter
    {
        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }
        public TaskCompletionSource<MqttConnectionContext> Context { get; } = new TaskCompletionSource<MqttConnectionContext>();

        public void Dispose()
        {
        }

        public async Task OnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535));
                var context = new MqttConnectionContext(formatter, connection);
                Context.TrySetResult(context);

                await ClientHandler(context);
            }
            catch (Exception ex)
            {
                Context.TrySetException(ex);
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
    }
}