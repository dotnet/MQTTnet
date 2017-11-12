using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace MQTTnet.TestApp.NetCore
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine($"MQTTnet - TestApp.{TargetFrameworkInfoProvider.TargetFramework}");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start server");
            Console.WriteLine("3 = Start performance test");
            Console.WriteLine("4 = Start managed client");

            var pressedKey = Console.ReadKey(true);
            if (pressedKey.KeyChar == '1')
            {
                Task.Run(ClientTest.RunAsync);
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
        
        // This code is used at the Wiki on GitHub!
        // ReSharper disable once UnusedMember.Local
        private static async void WikiCode()
        {
            {
                var client = new MqttFactory().CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("broker.hivemq.com")
                    .WithCredentials("bud", "%spencer%")
                    .WithTls()
                    .Build();

                await client.ConnectAsync(options);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("MyTopic")
                    .WithPayload("Hello World")
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                await client.PublishAsync(message);
            }

            {
                var factory = new MqttFactory();
                factory.GetLoggerFactory().AddConsole();

                var client = factory.CreateMqttClient();
            }
        }
    }

    public class RetainedMessageHandler : IMqttServerStorage
    {
        private const string Filename = "C:\\MQTT\\RetainedMessages.json";

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            var directory = Path.GetDirectoryName(Filename);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
