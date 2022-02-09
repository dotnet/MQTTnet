using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Load_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Handle_100_000_Messages_In_Receiving_Client()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var receivedMessages = 0;

                using (var receiverClient = await testEnvironment.ConnectClient())
                {
                    receiverClient.ApplicationMessageReceivedAsync += e =>
                    {
                        Interlocked.Increment(ref receivedMessages);
                        return Task.CompletedTask;
                    };

                    await receiverClient.SubscribeAsync("t/+");


                    for (var i = 0; i < 100; i++)
                    {
                        _ = Task.Run(
                            async () =>
                            {
                                using (var client = await testEnvironment.ConnectClient())
                                {
                                    var applicationMessageBuilder = new MqttApplicationMessageBuilder();

                                    for (var j = 0; j < 1000; j++)
                                    {
                                        var message = applicationMessageBuilder.WithTopic("t/" + j)
                                            .Build();

                                        await client.PublishAsync(message)
                                            .ConfigureAwait(false);
                                    }

                                    await client.DisconnectAsync();
                                }
                            });
                    }

                    SpinWait.SpinUntil(() => receivedMessages == 100000, TimeSpan.FromSeconds(60));

                    Assert.AreEqual(100000, receivedMessages);
                }
            }
        }

        [TestMethod]
        public async Task Handle_100_000_Messages_In_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var receivedMessages = 0;

                server.InterceptingPublishAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessages);
                    return Task.CompletedTask;
                };

                for (var i = 0; i < 100; i++)
                {
                    _ = Task.Run(
                        async () =>
                        {
                            using (var client = await testEnvironment.ConnectClient())
                            {
                                var applicationMessageBuilder = new MqttApplicationMessageBuilder();
                                
                                for (var j = 0; j < 1000; j++)
                                {
                                    var message = applicationMessageBuilder.WithTopic(j.ToString())
                                        .Build();

                                    await client.PublishAsync(message)
                                        .ConfigureAwait(false);
                                }

                                await client.DisconnectAsync();
                            }
                        });
                }

                SpinWait.SpinUntil(() => receivedMessages == 100000, TimeSpan.FromSeconds(60));

                Assert.AreEqual(100000, receivedMessages);
            }
        }
    }
}