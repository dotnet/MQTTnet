using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class Server_KeepAlive_Tests
    {
        [TestMethod]
        public async Task Disconnect_Client_DueTo_KeepAlive()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync();

                var client = await testEnvironment.ConnectLowLevelClientAsync(o => o.WithCommunicationTimeout(TimeSpan.FromSeconds(2))).ConfigureAwait(false);

                await client.SendAsync(new MqttConnectPacket
                {
                    CleanSession = true,
                    ClientId = "abc",
                    KeepAlivePeriod = 1,
                }, CancellationToken.None).ConfigureAwait(false);

                var response = await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(response is MqttConnAckPacket);

                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                await Task.Delay(500);
                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                await Task.Delay(500);
                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                await Task.Delay(500);
                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);

                // If we reach this point everything works as expected (server did not close the connection
                // due to proper ping messages.
                // Now we will wait 1.2 seconds because the server MUST wait 1.5 seconds in total (See spec).

                await Task.Delay(1200);
                await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);

                // Now we will wait longer than 1.5 so that the server will close the connection.

                await Task.Delay(3000);

                await server.StopAsync();

                await client.ReceiveAsync(CancellationToken.None);
            }
        }
    }
}