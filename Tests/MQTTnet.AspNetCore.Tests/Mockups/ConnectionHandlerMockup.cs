// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class ConnectionHandlerMockup: IMqttServerAdapter
    {
        public TaskCompletionSource<MqttConnectionContext> Context { get; } = new TaskCompletionSource<MqttConnectionContext>();
        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public async Task OnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                var writer = new SpanBasedMqttPacketWriter();
                var formatter = new MqttPacketFormatterAdapter(writer);
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

        public void Dispose()
        {
        }
    }
}
