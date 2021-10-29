using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public class Server_Tests : BaseTestClass
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage).WithProtocolVersion(MqttProtocolVersion.V500);

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                
                var receivedMessagesCount = 0;
                c1.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return Task.CompletedTask;
                };
                
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClient(clientOptions);
                c2.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Validate_IsSessionPresent()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                // Create server with persistent sessions enabled

                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                const string ClientId = "Client1";

                // Create client without clean session and long session expiry interval

                var client1 = await testEnvironment.ConnectClient(o => o
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithSessionExpiryInterval(9999)
                    .WithCleanSession(false)
                    .WithClientId(ClientId)
                    .Build()
                );

                // Disconnect; empty session should remain on server

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID to existing session

                var client2 = testEnvironment.CreateClient();
                var options = testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithSessionExpiryInterval(9999)
                    .WithCleanSession(false)
                    .WithClientId(ClientId)
                    .Build();


                var result = await client2.ConnectAsync(options).ConfigureAwait(false);

                await client2.DisconnectAsync();

                // Session should be present

                Assert.IsTrue(result.IsSessionPresent, "Session not present");
            }
        }
    }
}
