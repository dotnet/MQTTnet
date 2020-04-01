using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client.Options;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class LowLevelMqttClient_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Connect_And_Disconnect()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();

                var factory = new MqttFactory();
                var lowLevelClient = factory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                await lowLevelClient.DisconnectAsync(CancellationToken.None);
            }
        }

        [TestMethod]
        public async Task Authenticate()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();

                var factory = new MqttFactory();
                var lowLevelClient = factory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                var receivedPacket = await Authenticate(lowLevelClient).ConfigureAwait(false);

                await lowLevelClient.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual(MqttConnectReturnCode.ConnectionAccepted, receivedPacket.ReturnCode);
            }
        }

        [TestMethod]
        public async Task Subscribe()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();

                var factory = new MqttFactory();
                var lowLevelClient = factory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                await Authenticate(lowLevelClient).ConfigureAwait(false);

                var receivedPacket = await Subscribe(lowLevelClient, "a").ConfigureAwait(false);

                await lowLevelClient.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual(MqttSubscribeReturnCode.SuccessMaximumQoS0, receivedPacket.ReturnCodes[0]);
            }
        }

        async Task<MqttConnAckPacket> Authenticate(ILowLevelMqttClient client)
        {
            await client.SendAsync(new MqttConnectPacket()
            {
                CleanSession = true,
                ClientId = TestContext.TestName,
                Username = "user",
                Password = Encoding.UTF8.GetBytes("pass")
            },
            CancellationToken.None).ConfigureAwait(false);

            return await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false) as MqttConnAckPacket;
        }

        async Task<MqttSubAckPacket> Subscribe(ILowLevelMqttClient client, string topic)
        {
            await client.SendAsync(new MqttSubscribePacket
            {
                PacketIdentifier = 1,
                TopicFilters = new List<TopicFilter>
                {
                    new TopicFilter
                    {
                        Topic = topic
                    }
                }
            },
            CancellationToken.None).ConfigureAwait(false);

            return await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false) as MqttSubAckPacket;
        }
    }
}
