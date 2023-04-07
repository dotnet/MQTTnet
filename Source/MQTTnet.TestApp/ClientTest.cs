// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.TestApp
{
    public static class ClientTest
    {
        public static async Task RunAsync()
        {
            try
            {
                var logger = new MqttNetEventLogger();
                MqttNetConsoleLogger.ForwardToConsole(logger);

                var factory = new MqttFactory(logger);
                var client = factory.CreateMqttClient();
                var clientOptions = new MqttClientOptions
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "127.0.0.1"
                    }
                };

                client.ApplicationMessageReceivedAsync += e =>
                {
                    var payloadText = string.Empty;
                    if (e.ApplicationMessage.PayloadSegment.Count > 0)
                    {
                        payloadText = Encoding.UTF8.GetString(
                            e.ApplicationMessage.PayloadSegment.Array,
                            e.ApplicationMessage.PayloadSegment.Offset,
                            e.ApplicationMessage.PayloadSegment.Count);
                    }
                    
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {payloadText}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                    
                    return CompletedTask.Instance;
                };

                client.ConnectedAsync += async e =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    await client.SubscribeAsync("#");

                    Console.WriteLine("### SUBSCRIBED ###");
                };

                client.DisconnectedAsync += async e =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await client.ConnectAsync(clientOptions);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await client.ConnectAsync(clientOptions);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                while (true)
                {
                    Console.ReadLine();

                    await client.SubscribeAsync("test");

                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("A/B/C")
                        .WithPayload("Hello World")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await client.PublishAsync(applicationMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
