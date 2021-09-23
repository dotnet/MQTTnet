using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class KeepAlive_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Disconnect_Client_DueTo_KeepAlive()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectLowLevelClient(o => o
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(1))
                    .WithCommunicationTimeout(TimeSpan.Zero)
                    .WithProtocolVersion(MqttProtocolVersion.V500)).ConfigureAwait(false);

                await client.SendAsync(new MqttConnectPacket
                {
                    CleanSession = true,
                    ClientId = "Disconnect_Client_DueTo_KeepAlive",
                    KeepAlivePeriod = 1
                }, CancellationToken.None).ConfigureAwait(false);

                var responsePacket = await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(responsePacket is MqttConnAckPacket);

                for (var i = 0; i < 6; i++)
                {
                    await Task.Delay(500);
                    
                    await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                    responsePacket = await client.ReceiveAsync(CancellationToken.None);
                    Assert.IsTrue(responsePacket is MqttPingRespPacket);
                }
                
                // If we reach this point everything works as expected (server did not close the connection
                // due to proper ping messages.
                // Now we will wait 1.1 seconds because the server MUST wait 1.5 seconds in total (See spec).

                await Task.Delay(1100);
                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                responsePacket = await client.ReceiveAsync(CancellationToken.None);
                Assert.IsTrue(responsePacket is MqttPingRespPacket);

                // Now we will wait longer than 1.5 so that the server will close the connection.
                responsePacket = await client.ReceiveAsync(CancellationToken.None);

                var disconnectPacket = responsePacket as MqttDisconnectPacket;

                Assert.IsTrue(disconnectPacket != null);
                Assert.AreEqual(disconnectPacket.ReasonCode, MqttDisconnectReasonCode.KeepAliveTimeout);
            }
        }
    }
}