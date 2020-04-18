using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.TestApp.NetCore
{
    public static class Program
    {
        public static void Main()
        {
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

            Thread.Sleep(Timeout.Infinite);
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