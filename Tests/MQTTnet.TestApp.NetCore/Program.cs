using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Newtonsoft.Json;

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
            Console.WriteLine("5 = Start public broker test");
            Console.WriteLine("6 = Start server & client");
            Console.WriteLine("7 = Client flow test");
            Console.WriteLine("8 = Start performance test (client only)");
            Console.WriteLine("9 = Start server (no trace)");

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
                PerformanceTest.RunClientAndServer();
                return;
            }
            else if (pressedKey.KeyChar == '4')
            {
                Task.Run(ManagedClientTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '5')
            {
                Task.Run(PublicBrokerTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '6')
            {
                Task.Run(ServerAndClientTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '7')
            {
                Task.Run(ClientFlowTest.RunAsync);
            }
            else if (pressedKey.KeyChar == '8')
            {
                PerformanceTest.RunClientOnly();
                return;
            }
            else if (pressedKey.KeyChar == '9')
            {
                ServerTest.RunEmptyServer();
                return;
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
                var client = factory.CreateMqttClient();
            }

            {
                // Write all trace messages to the console window.
                MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                {
                    var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                    if (e.TraceMessage.Exception != null)
                    {
                        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                    }

                    Console.WriteLine(trace);
                };
            }

            {
                // Use a custom log ID for the logger.
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient(new MqttNetLogger("MyCustomId"));
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