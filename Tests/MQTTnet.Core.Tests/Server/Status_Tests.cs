using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Status_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Show_Client_And_Session_Statistics()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("client1"));
                var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("client2"));

                await Task.Delay(500);

                var clientStatus = await server.GetClientStatusAsync();
                var sessionStatus = await server.GetSessionStatusAsync();

                Assert.AreEqual(2, clientStatus.Count);
                Assert.AreEqual(2, sessionStatus.Count);

                Assert.IsTrue(clientStatus.Any(s => s.ClientId == c1.Options.ClientId));
                Assert.IsTrue(clientStatus.Any(s => s.ClientId == c2.Options.ClientId));

                await c1.DisconnectAsync();
                await c2.DisconnectAsync();

                await Task.Delay(500);

                clientStatus = await server.GetClientStatusAsync();
                sessionStatus = await server.GetSessionStatusAsync();

                Assert.AreEqual(0, clientStatus.Count);
                Assert.AreEqual(0, sessionStatus.Count);
            }
        }

        [TestMethod]
        public async Task Disconnect_Client()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("client1"));
                
                await Task.Delay(1000);

                var clientStatus = await server.GetClientStatusAsync();

                Assert.AreEqual(1, clientStatus.Count);
                Assert.IsTrue(clientStatus.Any(s => s.ClientId == c1.Options.ClientId));

                await clientStatus.First().DisconnectAsync();

                await Task.Delay(500);

                Assert.IsFalse(c1.IsConnected);

                clientStatus = await server.GetClientStatusAsync();

                Assert.AreEqual(0, clientStatus.Count);
            }
        }

        [TestMethod]
        public async Task Keep_Persistent_Session()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithPersistentSessions());

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("client1"));
                var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("client2"));

                await c1.DisconnectAsync();

                await Task.Delay(500);

                var clientStatus = await server.GetClientStatusAsync();
                var sessionStatus = await server.GetSessionStatusAsync();

                Assert.AreEqual(1, clientStatus.Count);
                Assert.AreEqual(2, sessionStatus.Count);

                await c2.DisconnectAsync();

                await Task.Delay(500);

                clientStatus = await server.GetClientStatusAsync();
                sessionStatus = await server.GetSessionStatusAsync();

                Assert.AreEqual(0, clientStatus.Count);
                Assert.AreEqual(2, sessionStatus.Count);
            }
        }

        [TestMethod]
        public async Task Track_Sent_Application_Messages()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithPersistentSessions());

                var c1 = await testEnvironment.ConnectClient();

                for (var i = 1; i < 25; i++)
                {
                    await c1.PublishAsync("a");
                    await Task.Delay(50);

                    var clientStatus = await server.GetClientStatusAsync();
                    Assert.AreEqual(i, clientStatus.First().SentApplicationMessagesCount);
                    Assert.AreEqual(0, clientStatus.First().ReceivedApplicationMessagesCount);
                }
            }
        }

        [TestMethod]
        public async Task Track_Sent_Packets()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithPersistentSessions());

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithNoKeepAlive());

                for (var i = 1; i < 25; i++)
                {
                    // At most once will send one packet to the client and the server will reply
                    // with an additional ACK packet.
                    await c1.PublishAsync("a", string.Empty, MqttQualityOfServiceLevel.AtLeastOnce);
                    
                    await Task.Delay(500);

                    var clientStatus = await server.GetClientStatusAsync();

                    Assert.AreEqual(i, clientStatus.First().SentApplicationMessagesCount, "SAMC invalid!");

                    // + 1 because CONNECT is also counted.
                    Assert.AreEqual(i + 1, clientStatus.First().SentPacketsCount, "SPC invalid!");

                    // +2 because ConnACK + PubAck package is already counted.
                    Assert.AreEqual(i + 2, clientStatus.First().ReceivedPacketsCount, "RPC invalid!");
                }
            }
        }
    }
}
