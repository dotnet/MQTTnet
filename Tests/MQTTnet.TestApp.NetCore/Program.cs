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
            var pressedKey = Console.ReadKey(true);
            if (pressedKey.Key == ConsoleKey.D1)
            {
                Task.Run(RunClientAsync);
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D2)
            {
                Task.Run(() => RunServerAsync());
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D3)
            {
                Task.Run(PerformanceTest.RunAsync);
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static async Task RunClientAsync()
        {
            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Console.WriteLine($">> [{DateTime.Now:O}] [{e.ThreadId}] [{e.Source}] [{e.Level}]: {e.Message}");
                if (e.Exception != null)
                {
                    Console.WriteLine(e.Exception);
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
                Console.WriteLine($">> [{e.ThreadId}] [{e.Source}] [{e.Level}]: {e.Message}");
                if (e.Exception != null)
                {
                    Console.WriteLine(e.Exception);
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


                var certificate = new X509Certificate(@"C:\certs\test\test.cer", "");
                options.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
                options.ConnectionBacklog = 5;
                options.DefaultEndpointOptions.IsEnabled = true;
                options.TlsEndpointOptions.IsEnabled = false;

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
