﻿using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics.Logger;
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
