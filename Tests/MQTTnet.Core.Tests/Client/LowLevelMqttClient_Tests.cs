using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Client
{
    [TestClass]
    public class LowLevelMqttClient_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Not_Existing_Server()
        {
            var client = new MqttFactory().CreateLowLevelMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .Build();

            await client.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Connect_And_Disconnect()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServer();

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
                await testEnvironment.StartServer();

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
                await testEnvironment.StartServer();

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

        [TestMethod]
        public async Task Loose_Connection()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.ServerPort = 8364;
                var server = await testEnvironment.StartServer();
                var client = await testEnvironment.ConnectLowLevelClient(o => o.WithCommunicationTimeout(TimeSpan.Zero));

                await Authenticate(client).ConfigureAwait(false);

                await server.StopAsync();

                await Task.Delay(1000);

                try
                {
                    await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None).ConfigureAwait(false);
                    await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None).ConfigureAwait(false);
                }
                catch (MqttCommunicationException exception)
                {
                    Assert.IsTrue(exception.InnerException is SocketException);
                    return;
                }
                catch
                {
                    Assert.Fail("Wrong exception type thrown.");
                }

                Assert.Fail("This MUST fail");
            }
        }

        async Task<MqttConnAckPacket> Authenticate(ILowLevelMqttClient client)
        {
            await client.SendAsync(new MqttConnectPacket
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
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilter
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
