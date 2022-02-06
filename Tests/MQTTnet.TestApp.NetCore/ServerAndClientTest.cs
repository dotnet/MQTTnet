// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.TestApp.NetCore
{
    public static class ServerAndClientTest
    {
        public static async Task RunAsync()
        {
            var logger = new MqttNetEventLogger();
            MqttNetConsoleLogger.ForwardToConsole(logger);

            var factory = new MqttFactory(logger);
            var server = factory.CreateMqttServer( new MqttServerOptionsBuilder().Build());
            var client = factory.CreateMqttClient();

            await server.StartAsync();

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();
            await client.ConnectAsync(clientOptions);

            await Task.Delay(Timeout.Infinite);
        }
    }
}
