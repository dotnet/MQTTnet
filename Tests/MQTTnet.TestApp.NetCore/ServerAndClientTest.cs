﻿using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.TestApp.NetCore
{
    public static class ServerAndClientTest
    {
        public static async Task RunAsync()
        {
            var logger = new MqttNetLogger();
            MqttNetConsoleLogger.ForwardToConsole(logger);

            var factory = new MqttFactory(logger);
            var server = factory.CreateMqttServer();
            var client = factory.CreateMqttClient();

            var serverOptions = new MqttServerOptionsBuilder().Build();
            await server.StartAsync(serverOptions);

            var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();
            await client.ConnectAsync(clientOptions);

            await Task.Delay(Timeout.Infinite);
        }
    }
}
