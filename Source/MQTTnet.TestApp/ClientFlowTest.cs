// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.TestApp
{
    public static class ClientFlowTest
    {
        public static async Task RunAsync()
        {
            try
            {
                var logger = new MqttNetEventLogger();
                MqttNetConsoleLogger.ForwardToConsole(logger);
                
                var factory = new MqttFactory(logger);
                
                var client = factory.CreateMqttClient();
                
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .Build();
                
                Console.WriteLine("BEFORE CONNECT");
                await client.ConnectAsync(options);
                Console.WriteLine("AFTER CONNECT");

                Console.WriteLine("BEFORE SUBSCRIBE");
                await client.SubscribeAsync("test/topic");
                Console.WriteLine("AFTER SUBSCRIBE");

                Console.WriteLine("BEFORE PUBLISH");
                await client.PublishStringAsync("test/topic", "payload");
                Console.WriteLine("AFTER PUBLISH");

                await Task.Delay(1000);

                Console.WriteLine("BEFORE DISCONNECT");
                await client.DisconnectAsync();
                Console.WriteLine("AFTER DISCONNECT");

                Console.WriteLine("FINISHED");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
