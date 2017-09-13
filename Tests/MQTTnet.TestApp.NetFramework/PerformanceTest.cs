using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.TestApp.NetFramework
{
    public static class PerformanceTest
    {
        public static async Task RunAsync()
        {
            var server = Task.Run(() => RunServerAsync());
            var client = Task.Run(() => RunClientAsync(1000, 50000, TimeSpan.FromMilliseconds(10)));

            await Task.WhenAll(server, client).ConfigureAwait(false);
        }

        private static async Task RunClientAsync(int messageChunkSize, int totalMessageCount, TimeSpan interval)
        {
            try
            {
                var options = new MqttClientOptions
                {
                    Server = "localhost",
                    ClientId = "XYZ",
                    CleanSession = true,
                    DefaultCommunicationTimeout = TimeSpan.FromMinutes(10)
                };

                var client = new MqttClientFactory().CreateMqttClient(options);
                client.ApplicationMessageReceived += (s, e) =>
                {
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
                        await client.ConnectAsync();
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                };

                try
                {
                    await client.ConnectAsync();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("### CONNECTING FAILED ###" + Environment.NewLine + exception);
                }

                Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");

                var applicationMessage = new MqttApplicationMessage(
                    "A/B/C",
                    Encoding.UTF8.GetBytes("Hello World"),
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    false
                );

                var overallCount = 0;
                while (overallCount < totalMessageCount)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var count = 0;
                    for (var i = 0; i < messageChunkSize; i++)
                    {
                        //do not await to send as much messages as possible 
                        await client.PublishAsync(applicationMessage).ConfigureAwait(false);
                        count++;
                        overallCount++;
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"Sent {count} messages within {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedMilliseconds / (float)count} ms / message).");
                    await Task.Delay(interval).ConfigureAwait(false);
                }

                Console.WriteLine($"Completed sending {totalMessageCount} messages.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static void RunServerAsync()
        {
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
                    },
                    DefaultCommunicationTimeout = TimeSpan.FromMinutes(10)
                };
                
                var mqttServer = new MqttServerFactory().CreateMqttServer(options);
                var last = DateTime.UtcNow;
                var msgs = 0;
                mqttServer.ApplicationMessageReceived += (sender, args) => 
                {
                    msgs++;
                    var now = DateTime.UtcNow;
                    if (last < now - TimeSpan.FromSeconds(1))
                    {
                        Console.WriteLine($"received {msgs}");
                        msgs = 0;
                        last = now;
                    }
                };
                mqttServer.Start();

                Console.WriteLine("Press any key to exit.");
                Console.ReadLine();

                mqttServer.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
    }
}
