using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;

namespace MQTTnet.TestApp.NetFramework
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("MQTTnet - TestApp.NetFramework");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start server");
            Console.WriteLine("3 = Start performance test");
            var pressedKey = Console.ReadKey(true);
            if (pressedKey.Key == ConsoleKey.D1)
            {
                Task.Run(() => RunClientAsync(args));
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D2)
            {
                Task.Run(() => RunServerAsync(args));
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D3)
            {
                Task.Run(PerformanceTest.RunAsync);
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static async Task RunClientAsync(string[] arguments)
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
                var options = new MqttClientWebSocketOptions
                {
                    Uri = "broker.hivemq.com:8000/mqtt"
                };

                ////var options = new MqttClientOptions
                ////{
                ////    Server = "localhost",
                ////    ClientId = "XYZ",
                ////    CleanSession = true
                ////};

                var mqttClient = new MqttClientFactory().CreateMqttClient();
                mqttClient.ApplicationMessageReceived += (s, e) =>
                {
                    Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
                    Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                    Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                    Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                    Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                    Console.WriteLine();
                };

                mqttClient.Connected += async (s, e) =>
                {
                    Console.WriteLine("### CONNECTED WITH SERVER ###");

                    await mqttClient.SubscribeAsync(new List<TopicFilter>
                    {
                        new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce)
                    });
                };

                mqttClient.Disconnected += async (s, e) =>
                {
                    Console.WriteLine("### DISCONNECTED FROM SERVER ###");
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    try
                    {
                        await mqttClient.ConnectAsync(options);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await mqttClient.ConnectAsync(options);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                var messageFactory = new MqttApplicationMessageFactory();
                while (true)
                {
                    Console.ReadLine();

                    var applicationMessage = messageFactory.CreateApplicationMessage("myTopic", "Hello World", MqttQualityOfServiceLevel.AtLeastOnce);
                    await mqttClient.PublishAsync(applicationMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static async Task RunServerAsync(string[] arguments)
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

                var mqttServer = new MqttServerFactory().CreateMqttServer(options);
                await mqttServer.StartAsync();

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                await mqttServer.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }

        private static async Task WikiCode()
        {
            // For .NET Framwork & netstandard apps:
            MqttTcpChannel.CustomCertificateValidationCallback = (x509Certificate, x509Chain, sslPolicyErrors, mqttClientTcpOptions) =>
            {
                if (mqttClientTcpOptions.Server == "server_with_revoked_cert")
                {
                    return true;
                }

                return false;
            };
        }
    }
}