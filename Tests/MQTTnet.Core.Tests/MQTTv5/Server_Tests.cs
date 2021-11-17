using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public class Server_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage).WithProtocolVersion(MqttProtocolVersion.V500);

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
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
                var options1 = CreateClientOptions(testEnvironment, ClientId, false, 9999);
                var client1 = await testEnvironment.ConnectClient(options1);

                // Disconnect; empty session should remain on server

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID to existing session

                var client2 = testEnvironment.CreateClient();
                var options2 = CreateClientOptions(testEnvironment, ClientId, false, 9999);
                var result = await client2.ConnectAsync(options2).ConfigureAwait(false);

                await client2.DisconnectAsync();

                // Session should be present

                Assert.IsTrue(result.IsSessionPresent, "Session not present");
            }
        }

        [TestMethod]
        public async Task Connect_with_Undefined_SessionExpiryInterval()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                // Create server with persistent sessions enabled

                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                const string ClientId = "Client1";

                // Create client without clean session and NO session expiry interval,
                // that means, the session should not persist

                var options1 = CreateClientOptions(testEnvironment, ClientId, false, null);
                var client1 = await testEnvironment.ConnectClient(options1);

                // Disconnect; no session should remain on server because the session expiry interval was undefined

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID to existing session

                var client2 = testEnvironment.CreateClient();
                var options2 = CreateClientOptions(testEnvironment, ClientId, false, 9999);
                var result = await client2.ConnectAsync(options2).ConfigureAwait(false);

                await client2.DisconnectAsync();

                // Session should not be present

                Assert.IsTrue(!result.IsSessionPresent, "Session is present when it should not");
            }
        }


        [TestMethod]
        public async Task Reconnect_with_different_SessionExpiryInterval()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                // Create server with persistent sessions enabled

                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                const string ClientId = "Client1";

                // Create client with clean session and session expiry interval > 0

                var options = CreateClientOptions(testEnvironment, ClientId, true, 9999);
                var client1 = await testEnvironment.ConnectClient(options);

                // Disconnect; session should remain on server

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID to the existing session but leave session expiry interval undefined this time.
                // Session should be present because the client1 connection had SessionExpiryInterval > 0

                var client2 = testEnvironment.CreateClient();
                var options2 = CreateClientOptions(testEnvironment, ClientId, false, null);
                var result2 = await client2.ConnectAsync(options2).ConfigureAwait(false);

                await client2.DisconnectAsync();

                Assert.IsTrue(result2.IsSessionPresent, "Session is not present when it should");

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID.
                // No session should be present because the previous session expiry interval was undefined for the client2 connection

                var client3 = testEnvironment.CreateClient();
                var options3 = CreateClientOptions(testEnvironment, ClientId, false, null);
                var result3 = await client2.ConnectAsync(options3).ConfigureAwait(false);

                await client3.DisconnectAsync();

                // Session should be present

                Assert.IsTrue(!result3.IsSessionPresent, "Session is present when it should not");

            }
        }

        IMqttClientOptions CreateClientOptions(TestEnvironment testEnvironment, string clientId, bool cleanSession, uint? sessionExpiryInterval)
        {
            return testEnvironment.Factory.CreateClientOptionsBuilder()
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                .WithSessionExpiryInterval(sessionExpiryInterval)
                .WithCleanSession(cleanSession)
                .WithClientId(clientId)
                .Build();
        }
    }
}
