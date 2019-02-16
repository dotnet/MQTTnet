using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public class ManagedMqttClientTests
    {
        [TestMethod]
        public async Task Drop_New_Messages_On_Full_Queue()
        {
            var factory = new MqttFactory();
            var managedClient = factory.CreateManagedMqttClient();
            try
            {
                var clientOptions = new ManagedMqttClientOptionsBuilder()
                    .WithMaxPendingMessages(5)
                    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropNewMessage);

                clientOptions.WithClientOptions(o => o.WithTcpServer("localhost"));

                await managedClient.StartAsync(clientOptions.Build());

                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "1" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "2" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "3" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "4" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "5" });

                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "6" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "7" });
                await managedClient.PublishAsync(new MqttApplicationMessage { Topic = "8" });

                Assert.AreEqual(5, managedClient.PendingApplicationMessagesCount);
            }
            finally
            {
                await managedClient.StopAsync();
            }
        }

        [TestMethod]
        public async Task ManagedClients_Will_Message_Send()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                var receivedMessagesCount = 0;

                var factory = new MqttFactory();

                await testEnvironment.StartServerAsync();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort)
                    .WithWillMessage(willMessage);
                var dyingClient = testEnvironment.CreateClient();
                var dyingManagedClient = new ManagedMqttClient(dyingClient, testEnvironment.ClientLogger.CreateChildLogger());
                await dyingManagedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .Build());

                var recievingClient = await testEnvironment.ConnectClientAsync();
                await recievingClient.SubscribeAsync("My/last/will");

                recievingClient.UseReceivedApplicationMessageHandler(context => Interlocked.Increment(ref receivedMessagesCount));

                dyingManagedClient.Dispose();

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Start_Stop()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                var factory = new MqttFactory();

                var server = await testEnvironment.StartServerAsync();

                var managedClient = new ManagedMqttClient(testEnvironment.CreateClient(), new MqttNetLogger().CreateChildLogger());
                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort);

                TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
                managedClient.Connected += (s, e) => { connected.SetResult(true); };

                await managedClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                    .WithClientOptions(clientOptions)
                    .Build());

                await connected.Task;

                await managedClient.StopAsync();

                Assert.AreEqual(0, (await server.GetClientStatusAsync()).Count);
            }
        }
    }
}