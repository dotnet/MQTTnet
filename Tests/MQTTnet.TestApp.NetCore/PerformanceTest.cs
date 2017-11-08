using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.TestApp.NetCore
{
    public static class PerformanceTest
    {
        public static async Task RunAsync()
        {
            var services = new ServiceCollection()
                .AddMqttServer(options =>
                {

                    options.ConnectionValidator = p =>
                    {
                        if (p.ClientId == "SpecialClient")
                        {
                            if (p.Username != "USER" || p.Password != "PASS")
                            {
                                return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                            }
                        }

                        return MqttConnectReturnCode.ConnectionAccepted;
                    };

                    options.DefaultCommunicationTimeout = TimeSpan.FromMinutes(10);
                })
                .AddMqttClient()
                .AddLogging()
                .BuildServiceProvider();

            //services.GetService<ILoggerFactory>().AddConsole(LogLevel.Warning, true);

            Console.WriteLine("Press 'c' for concurrent sends. Otherwise in one batch.");
            var concurrent = Console.ReadKey(true).KeyChar == 'c';

            var server = Task.Factory.StartNew(() => RunServerAsync(services), TaskCreationOptions.LongRunning);
            var client = Task.Factory.StartNew(() => RunClientAsync(2000, TimeSpan.FromMilliseconds(10), services, concurrent), TaskCreationOptions.LongRunning);

            await Task.WhenAll(server, client).ConfigureAwait(false);
        }

        private static Task RunClientsAsync(int msgChunkSize, TimeSpan interval, IServiceProvider serviceProvider, bool concurrent)
        {
            return Task.WhenAll(Enumerable.Range(0, 3).Select(i => Task.Run(() => RunClientAsync(msgChunkSize, interval, serviceProvider, concurrent))));
        }

        private static async Task RunClientAsync(int msgChunkSize, TimeSpan interval, IServiceProvider serviceProvider, bool concurrent)
        {
            try
            {
                var options = new MqttClientOptions
                {
                    ChannelOptions = new MqttClientTcpOptions { Server = "localhost" },
                    ClientId = "Client1",
                    CleanSession = true,
                    CommunicationTimeout = TimeSpan.FromMinutes(10)
                };

                var client = serviceProvider.GetRequiredService<IMqttClient>();

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

                var testMessageCount = 10000;
                var message = CreateMessage();
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < testMessageCount; i++)
                {
                    await client.PublishAsync(message);
                }

                stopwatch.Stop();
                Console.WriteLine($"Sent 10.000 messages within {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedMilliseconds / (float)testMessageCount} ms / message).");


                var messages = new[] { message };
                var sentMessagesCount = 0;

                stopwatch.Restart();

                while (stopwatch.ElapsedMilliseconds < 1000)
                {
                    await client.PublishAsync(messages).ConfigureAwait(false);
                    sentMessagesCount++;
                }

                Console.WriteLine($"Sending {sentMessagesCount} messages per second.");

                var last = DateTime.Now;
                var msgCount = 0;

                while (true)
                {
                    var msgs = Enumerable.Range(0, msgChunkSize)
                        .Select(i => CreateMessage())
                        .ToList();

                    if (concurrent)
                    {
                        //send concurrent (test for raceconditions)
                        var sendTasks = msgs
                            .Select(msg => PublishSingleMessage(client, msg, ref msgCount))
                            .ToList();

                        await Task.WhenAll(sendTasks);
                    }
                    else
                    {
                        await client.PublishAsync(msgs);
                        msgCount += msgs.Count;
                        //send multiple
                    }



                    var now = DateTime.Now;
                    if (last < now - TimeSpan.FromSeconds(1))
                    {
                        Console.WriteLine($"sending {msgCount} intended {msgChunkSize / interval.TotalSeconds}");
                        msgCount = 0;
                        last = now;
                    }

                    await Task.Delay(interval).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static MqttApplicationMessage CreateMessage()
        {
            return new MqttApplicationMessage
            {
                Topic = "A/B/C",
                Payload = Encoding.UTF8.GetBytes("Hello World"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };
        }

        private static Task PublishSingleMessage(IMqttClient client, MqttApplicationMessage applicationMessage, ref int count)
        {
            Interlocked.Increment(ref count);
            return Task.Run(() => client.PublishAsync(applicationMessage));
        }

        private static async Task RunServerAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var mqttServer = serviceProvider.GetRequiredService<IMqttServer>();
                var msgs = 0;
                var stopwatch = Stopwatch.StartNew();
                mqttServer.ApplicationMessageReceived += (sender, args) =>
                {
                    msgs++;
                    if (stopwatch.ElapsedMilliseconds > 1000)
                    {
                        Console.WriteLine($"received {msgs}");
                        msgs = 0;
                        stopwatch.Restart();
                    }
                };
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
    }
}
