using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
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
                Task.Run(RunServerAsync);
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
            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                if (e.TraceMessage.Exception != null)
                {
                    Console.WriteLine(e.TraceMessage.Exception);
                }
            };

            try
            {
                var options = new MqttClientWebSocketOptions
                {
                    Uri = "localhost",
                    ClientId = "XYZ",
                    CleanSession = true
                };

                var client = new MqttClientFactory().CreateMqttClient();
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

                    var applicationMessage = new MqttApplicationMessage(
                        "A/B/C",
                        Encoding.UTF8.GetBytes("Hello World"),
                        MqttQualityOfServiceLevel.AtLeastOnce,
                        false
                    );

                    await client.PublishAsync(applicationMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static Task RunServerAsync()
        {
            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                if (e.TraceMessage.Exception != null)
                {
                    Console.WriteLine(e.TraceMessage.Exception);
                }
            };

            try
            {
                var options = new MqttServerOptions
                {
                    ConnectionValidator = p =>
                    {
                        if (p.ClientId == "SpecialClient")
                        {
                            if (p.Username != "USER" || p.Password != "PASS")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }
                        }

                        return MqttConnectReturnCode.ConnectionAccepted;
                    }
                };

                options.Storage = new RetainedMessageHandler();


                //var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                //options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                //options.ConnectionBacklog = 5;
                //options.DefaultEndpointOptions.IsEnabled = true;
                //options.TlsEndpointOptions.IsEnabled = false;

                var mqttServer = new MqttServerFactory().CreateMqttServer(options);
                mqttServer.ClientDisconnected += (s, e) =>
                {
                    Console.Write("Client disconnected event fired.");
                };

                mqttServer.StartAsync();

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                mqttServer.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
            return Task.FromResult(0);
        }

        // ReSharper disable once UnusedMember.Local
        private static async void WikiCode()
        {
            {
                var client = new MqttClientFactory().CreateMqttClient(new CustomTraceHandler("Client 1"));

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

    public class CustomTraceHandler : IMqttNetTraceHandler
    {
        private readonly string _clientId;

        public CustomTraceHandler(string clientId)
        {
            _clientId = clientId;
        }

        public bool IsEnabled { get; } = true;

        public void HandleTraceMessage(MqttNetTraceMessage traceMessage)
        {
            // Client ID is added to the trace message.
            Console.WriteLine($">> [{_clientId}] [{traceMessage.Timestamp:O}] [{traceMessage.ThreadId}] [{traceMessage.Source}] [{traceMessage.Level}]: {traceMessage.Message}");
            if (traceMessage.Exception != null)
            {
                Console.WriteLine(traceMessage.Exception);
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
