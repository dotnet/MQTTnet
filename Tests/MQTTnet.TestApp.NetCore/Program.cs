using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MQTTnet.TestApp.NetCore
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("MQTTnet - TestApp.NetFramework");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start server");
            Console.WriteLine("3 = Start performance test");
            Console.WriteLine("4 = Start managed client");
                        
            var pressedKey = Console.ReadKey(true);
            if (pressedKey.KeyChar == '1')
            {
                Task.Run(RunClientAsync);
            }
            else if (pressedKey.KeyChar == '2')
            {
                Task.Run(ServerTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '3')
            {
                Task.Run(PerformanceTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '4')
            {
                Task.Run(ManagedClientTest.RunAsync);
            }

            Thread.Sleep(Timeout.Infinite);
        }

        private static async Task RunClientAsync()
        {
            try
            {
                var services = new ServiceCollection()
                    .AddMqttServer()
                    .AddLogging()
                    .BuildServiceProvider();

                services.GetService<ILoggerFactory>()
                    .AddConsole();

                var options = new MqttClientWebSocketOptions
                {
                    Uri = "localhost",
                    ClientId = "XYZ",
                    CleanSession = true
                };

                var client = services.GetRequiredService<IMqttClient>();
                client.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                };

                client.Connected += async (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    await client.SubscribeAsync(new List<TopicFilter>
                    {
                        new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce)
                    });

                    Console.WriteLine("### SUBSCRIBED ###");
                };

                client.Disconnected += async (s, e) =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await client.ConnectAsync(options);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await client.ConnectAsync(options);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                while (true)
                {
                    Console.ReadLine();

                    var applicationMessage = new MqttApplicationMessage
                    {
                        Topic = "A/B/C",
                        Payload = Encoding.UTF8.GetBytes("Hello World"),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce
                    };

                    await client.PublishAsync(applicationMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private static async void WikiCode()
        {
            {
                var serviceProvider = new ServiceCollection()
                    .AddMqttServer()
                    .AddLogging()
                    .BuildServiceProvider();

                var client = serviceProvider.GetRequiredService<IMqttClient>();

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("MyTopic")
                    .WithPayload("Hello World")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                await client.PublishAsync(message);
            }

            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("/MQTTnet/is/awesome")
                    .Build();
            }
        }
    }

    public class RetainedMessageHandler : IMqttServerStorage
    {
        private const string Filename = "C:\\MQTT\\RetainedMessages.json";

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            File.WriteAllText(Filename, JsonConvert.SerializeObject(messages));
            return Task.FromResult(0);
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            IList<MqttApplicationMessage> retainedMessages;
            if (File.Exists(Filename))
            {
                var json = File.ReadAllText(Filename);
                retainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
            }
            else
            {
                retainedMessages = new List<MqttApplicationMessage>();
            }

            return Task.FromResult(retainedMessages);
        }
    }
}
