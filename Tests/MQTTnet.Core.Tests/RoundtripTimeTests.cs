using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;

namespace MQTTnet.Tests
{
    [TestClass]
    public class RoundtripTimeTests
    {
        [TestMethod]
        public async Task Round_Trip_Time()
        {
            var factory = new MqttFactory();
            var server = factory.CreateMqttServer();
            var receiverClient = factory.CreateMqttClient();
            var senderClient = factory.CreateMqttClient();

            await server.StartAsync(new MqttServerOptionsBuilder().Build());

            await receiverClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());
            await receiverClient.SubscribeAsync("#");

            await senderClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());

            TaskCompletionSource<string> response = null;

            receiverClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
            {
                response?.SetResult(args.ApplicationMessage.ConvertPayloadToString());
            });

            var times = new List<TimeSpan>();
            var stopwatch = Stopwatch.StartNew();

            await Task.Delay(1000);

            for (var i = 0; i < 100; i++)
            {
                response = new TaskCompletionSource<string>();
                await senderClient.PublishAsync("test", DateTime.UtcNow.Ticks.ToString());
                response.Task.GetAwaiter().GetResult();

                stopwatch.Stop();
                times.Add(stopwatch.Elapsed);
                stopwatch.Restart();
            }

            await server.StopAsync();
        }
    }
}
