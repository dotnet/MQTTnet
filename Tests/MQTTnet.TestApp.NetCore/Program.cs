using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Implementations;

namespace MQTTnet.TestApp.NetCore
{
    public static class Program
    {
        public static void Main()
        {
            //MqttNetConsoleLogger.ForwardToConsole();

            Console.WriteLine($"MQTTnet - TestApp.{TargetFrameworkProvider.TargetFramework}");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start server");
            Console.WriteLine("3 = Start performance test");
            Console.WriteLine("4 = Start managed client");
            Console.WriteLine("5 = Start public broker test");
            Console.WriteLine("6 = Start server & client");
            Console.WriteLine("7 = Client flow test");
            Console.WriteLine("8 = Start performance test (client only)");
            Console.WriteLine("9 = Start server (no trace)");
            Console.WriteLine("a = Start QoS 2 benchmark");
            Console.WriteLine("b = Start QoS 1 benchmark");
            Console.WriteLine("c = Start QoS 0 benchmark");
            Console.WriteLine("d = Start server with logging");
            Console.WriteLine("e = Start Message Throughput Test");

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
                Task.Run(PerformanceTest.RunClientAndServer);
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
            else if (pressedKey.KeyChar == 'a')
            {
                Task.Run(PerformanceTest.RunQoS2Test);
            }
            else if (pressedKey.KeyChar == 'b')
            {
                Task.Run(PerformanceTest.RunQoS1Test);
            }
            else if (pressedKey.KeyChar == 'c')
            {
                Task.Run(PerformanceTest.RunQoS0Test);
            }
            else if (pressedKey.KeyChar == 'd')
            {
                Task.Run(ServerTest.RunEmptyServerWithLogging);
            }
            else if (pressedKey.KeyChar == 'e')
            {
                Task.Run(new MessageThroughputTest().Run);
            }

            Thread.Sleep(Timeout.Infinite);
        }

        static int _count;

        static async Task ClientTestWithHandlers()
        {
            //private static int _count = 0;

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("mqttnetspeed")
                .WithTcpServer("#serveraddress#")
                .WithCredentials("#username#", "#password#")
                .WithCleanSession()
                .Build();

            //mqttClient.ApplicationMessageReceived += (s, e) =>    // version 2.8.5
            mqttClient.ApplicationMessageReceivedAsync += e =>    // version 3.0.0+
            {
                Interlocked.Increment(ref _count);
                return PlatformAbstractionLayer.CompletedTask;
            };

            //mqttClient.Connected += async (s, e) =>               // version 2.8.5
            mqttClient.UseConnectedHandler(async e =>               // version 3.0.0+
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");
                await mqttClient.SubscribeAsync("topic/+", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                Console.WriteLine("### SUBSCRIBED ###");
            });

            await mqttClient.ConnectAsync(options);

            while (true)
            {
                Console.WriteLine($"{Interlocked.Exchange(ref _count, 0)}/s");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

        }
    }
    
    public class WikiCode
    {
        public void Code()
        {
            //Validate certificate.
            var options = new MqttClientOptionsBuilder()
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    CertificateValidationHandler = context =>
                        {
                            // TODO: Check conditions of certificate by using above context.
                            if (context.SslPolicyErrors == SslPolicyErrors.None)
                            {
                                return true;
                            }

                            return false;
                        }
                })
                .Build();
        }
    }
}