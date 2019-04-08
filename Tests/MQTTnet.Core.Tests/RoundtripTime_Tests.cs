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
        [TestMethod]
        public async Task Round_Trip_Time()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServerAsync();
                var receiverClient = await testEnvironment.ConnectClientAsync();
                var senderClient = await testEnvironment.ConnectClientAsync();

                await receiverClient.SubscribeAsync("#");
                
                TaskCompletionSource<string> response = null;

                receiverClient.UseApplicationMessageReceivedHandler(e => 
                {
                    response?.SetResult(e.ApplicationMessage.ConvertPayloadToString());
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
            }
        }
    }
}
