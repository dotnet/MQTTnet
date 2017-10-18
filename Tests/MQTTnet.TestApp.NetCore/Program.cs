using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.TestApp.NetCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("MQTTnet - TestApp.NetFramework");
            Console.WriteLine("1 = Start client");
            Console.WriteLine("2 = Start queued client");
            Console.WriteLine("3 = Start server");
            Console.WriteLine("4 = Start performance test");
            var pressedKey = Console.ReadKey(true);
            if (pressedKey.Key == ConsoleKey.D1)
            {
                Task.Run(() => RunClientAsync(args));
                Thread.Sleep(Timeout.Infinite);
            }
            if (pressedKey.Key == ConsoleKey.D2)
            {
                Task.Run(() => RunClientQueuedAsync(args));
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D3)
            {
                Task.Run(() => RunServerAsync(args));
                Thread.Sleep(Timeout.Infinite);
            }
            else if (pressedKey.Key == ConsoleKey.D4)
            {
                Task.Run(PerformanceTest.RunAsync);
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static async Task RunClientQueuedAsync(string[] arguments)
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
                var options = new MqttClientQueuedOptions
                {
                    Server = "192.168.0.14",
                    ClientId = "XYZ",
                    CleanSession = true,
                    UserName = "lobu",
                    Password = "passworda",
                    KeepAlivePeriod = TimeSpan.FromSeconds(31),
                    DefaultCommunicationTimeout = TimeSpan.FromSeconds(20),
                    UsePersistence = true,
                    Storage = new TestStorage(),
                };

                var client = new MqttClientFactory().CreateMqttQueuedClient();
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

                int i = 0;
                while (true)
                {
                    Console.ReadLine();
                    i++;
                    var applicationMessage = new MqttApplicationMessage(
                        "PLNMAIN",
                        Encoding.UTF8.GetBytes(string.Format("Hello World {0}", i)),
                        MqttQualityOfServiceLevel.ExactlyOnce,
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
                    Uri = "localhost",
                    ClientId = "XYZ",
                    CleanSession = true,
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

        private static void RunServerAsync(string[] arguments)
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
        }

        [Serializable]
        public sealed class TemporaryApplicationMessage
        {
            public TemporaryApplicationMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfServiceLevel, bool retain)
            {
                Topic = topic ?? throw new ArgumentNullException(nameof(topic));
                Payload = payload ?? throw new ArgumentNullException(nameof(payload));
                QualityOfServiceLevel = qualityOfServiceLevel;
                Retain = retain;
            }

            public string Topic { get; }

            public byte[] Payload { get; }

            public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }

            public bool Retain { get; }
        }

        private class TestStorage : IMqttClientQueuedStorage
        {
            string serializationFile = System.IO.Path.Combine(Environment.CurrentDirectory, "messages.bin");
            private IList<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();

            public Task<IList<MqttApplicationMessage>> LoadInflightMessagesAsync()
            {
                //deserialize
                // MqttApplicationMessage is not serializable
                if (System.IO.File.Exists(serializationFile))
                {
                    using (System.IO.Stream stream = System.IO.File.Open(serializationFile, System.IO.FileMode.Open))
                    {
                        var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        var temp = (List<TemporaryApplicationMessage>)bformatter.Deserialize(stream);
                        foreach (var a in temp)
                        {
                            _messages.Add(new MqttApplicationMessage(a.Topic, a.Payload, a.QualityOfServiceLevel, a.Retain));
                        }
                    }
                }
                return Task.FromResult(_messages);
            }

            public Task SaveInflightMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                _messages = messages;
                //serialize
                // MqttApplicationMessage is not serializable
                using (System.IO.Stream stream = System.IO.File.Open(serializationFile, System.IO.FileMode.Create))
                {
                    var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    IList<TemporaryApplicationMessage> temp = new List<TemporaryApplicationMessage>();
                    foreach (var m in _messages)
                    {
                        temp.Add(new TemporaryApplicationMessage(m.Topic, m.Payload, m.QualityOfServiceLevel, m.Retain));
                    }
                    bformatter.Serialize(stream, temp);
                }

                return Task.FromResult(0);
            }
        }
    }
}
