using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public class RoundtripTime_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Round_Trip_Time()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();
                var receiverClient = await testEnvironment.ConnectClient();
                var senderClient = await testEnvironment.ConnectClient();

                await receiverClient.SubscribeAsync("#");
                
                TaskCompletionSource<string> response = null;

                receiverClient.ApplicationMessageReceivedAsync += e => 
                {
                    response?.SetResult(e.ApplicationMessage.ConvertPayloadToString());
                    return Task.CompletedTask;
                };

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
            }
        }
    }
}
