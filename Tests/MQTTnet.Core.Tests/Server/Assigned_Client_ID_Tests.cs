using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Assigned_Client_ID_Tests : BaseTestClass
    {
        [TestMethod]
        public Task Connect_With_No_Client_Id()
        {
            return Connect_With_Client_Id("test_123", "test_123", null, "test_123");
        }

        [TestMethod]
        public Task Connect_With_Client_Id()
        {
            return Connect_With_Client_Id("Connect_With_Client_Id_test_456", null, "test_456", null);
        }

        async Task Connect_With_Client_Id(string expectedClientId, string expectedReturnedClientId, string usedClientId, string assignedClientId)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                string serverConnectedClientId = null;
                string serverDisconnectedClientId = null;
                string clientAssignedClientId = null;

                // Arrange server
                var disconnectedMre = new ManualResetEventSlim();

                var server = await testEnvironment.StartServer();
                server.ValidatingConnectionAsync += e =>
                {
                    if (string.IsNullOrEmpty(e.ClientId))
                    {
                        e.AssignedClientIdentifier = assignedClientId;
                        e.ReasonCode = MqttConnectReasonCode.Success;
                    }
                    
                    return Task.CompletedTask;
                };
                
                testEnvironment.Server.ClientConnectedAsync += args =>
                {
                    serverConnectedClientId = args.ClientId;
                    return Task.CompletedTask;
                };

                testEnvironment.Server.ClientDisconnectedAsync += args =>
                {
                    serverDisconnectedClientId = args.ClientId;
                    disconnectedMre.Set();
                    return Task.CompletedTask;
                };

                // Arrange client
                var client = testEnvironment.CreateClient();
                client.UseConnectedHandler(args =>
                {
                    clientAssignedClientId = args.ConnectResult.AssignedClientIdentifier;
                });

                // Act
                await client.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithClientId(usedClientId)
                    .Build());

                await client.DisconnectAsync();

                // Wait for ClientDisconnectedHandler to trigger
                disconnectedMre.Wait(1000);

                // Assert
                Assert.AreEqual(expectedClientId, serverConnectedClientId);
                Assert.AreEqual(expectedClientId, serverDisconnectedClientId);
                Assert.AreEqual(expectedReturnedClientId, clientAssignedClientId);
            }
        }
    }
}