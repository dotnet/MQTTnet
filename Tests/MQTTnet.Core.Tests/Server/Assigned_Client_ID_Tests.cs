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
        public async Task Connect_With_Assigned_Client_Id()
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
                            context.AssignedClientIdentifier = "test123";
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
                    .WithClientId(null)
                    .Build());
                await client.DisconnectAsync();

                // Wait for ClientDisconnectedHandler to trigger
                disconnectedMre.Wait(500);

                // Assert
                Assert.IsNotNull(serverConnectedClientId);
                Assert.IsNotNull(serverDisconnectedClientId);
                Assert.IsNotNull(clientAssignedClientId);
                Assert.AreEqual("test123", serverConnectedClientId);
                Assert.AreEqual("test123", serverDisconnectedClientId);
                Assert.AreEqual("test123", clientAssignedClientId);
            }
        }
    }
}