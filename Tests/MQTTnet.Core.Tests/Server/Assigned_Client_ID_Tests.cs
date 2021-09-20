using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

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
            return Connect_With_Client_Id("Connect_With_Client_Idtest_456", null, "test_456", null);
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
                var serverOptions = new MqttServerOptionsBuilder()
                    .WithConnectionValidator(context =>
                    {
                        if (string.IsNullOrEmpty(context.ClientId))
                        {
                            context.AssignedClientIdentifier = assignedClientId;
                            context.ReasonCode = MqttConnectReasonCode.Success;
                        }
                    });

                await testEnvironment.StartServer(serverOptions);
                testEnvironment.Server.UseClientConnectedHandler(args =>
                {
                    serverConnectedClientId = args.ClientId;
                });

                testEnvironment.Server.UseClientDisconnectedHandler(args =>
                {
                    serverDisconnectedClientId = args.ClientId;
                    disconnectedMre.Set();
                });

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