// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.LowLevelClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests.Clients.LowLevelMqttClient
{
    [TestClass]
    public sealed class LowLevelMqttClient_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Authenticate()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var factory = new MqttClientFactory();
                var lowLevelClient = factory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                var receivedPacket = await Authenticate(lowLevelClient).ConfigureAwait(false);

                await lowLevelClient.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual(MqttConnectReturnCode.ConnectionAccepted, receivedPacket.ReturnCode);
            }
        }

        [TestMethod]
        public async Task Connect_And_Disconnect()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var lowLevelClient = testEnvironment.ClientFactory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                await lowLevelClient.DisconnectAsync(CancellationToken.None);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Not_Existing_Broker()
        {
            var client = new MqttClientFactory().CreateLowLevelMqttClient();
            var options = new MqttClientOptionsBuilder().WithTcpServer("localhost").Build();

            await client.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public async Task Connect_To_Wrong_Host()
        {
            var client = new MqttClientFactory().CreateLowLevelMqttClient();
            var options = new MqttClientOptionsBuilder().WithTcpServer("123.456.789.10").Build();

            await client.ConnectAsync(options, CancellationToken.None).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Loose_Connection()
        {
            using var testEnvironments = CreateMQTTnetTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                testEnvironment.IgnoreServerLogErrors = true;

                testEnvironment.ServerPort = 8364;
                var server = await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectLowLevelClient(o => o.WithTimeout(TimeSpan.Zero));

                await Authenticate(client).ConfigureAwait(false);

                await server.StopAsync();

                await Task.Delay(2000);

                try
                {
                    await client.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None).ConfigureAwait(false);
                    await Task.Delay(2000);
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

        [TestMethod]
        public async Task Maintain_IsConnected_Property()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                testEnvironment.IgnoreServerLogErrors = true;

                var server = await testEnvironment.StartServer();

                using (var lowLevelClient = testEnvironment.CreateLowLevelClient())
                {
                    Assert.IsFalse(lowLevelClient.IsConnected);

                    var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).WithTimeout(TimeSpan.FromSeconds(1)).Build();

                    await lowLevelClient.ConnectAsync(clientOptions, CancellationToken.None);

                    Assert.IsTrue(lowLevelClient.IsConnected);

                    await server.StopAsync();
                    server.Dispose();

                    await LongTestDelay();

                    Assert.IsTrue(lowLevelClient.IsConnected);

                    try
                    {
                        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                        {
                            await lowLevelClient.SendAsync(MqttPingReqPacket.Instance, CancellationToken.None);
                            await LongTestDelay();

                            await lowLevelClient.ReceiveAsync(timeout.Token);
                        }
                    }
                    catch
                    {
                    }

                    Assert.IsFalse(lowLevelClient.IsConnected);
                }
            }
        }

        [TestMethod]
        public async Task Subscribe()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var factory = new MqttClientFactory();
                var lowLevelClient = factory.CreateLowLevelMqttClient();

                await lowLevelClient.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", testEnvironment.ServerPort).Build(), CancellationToken.None);

                await Authenticate(lowLevelClient).ConfigureAwait(false);

                var receivedPacket = await Subscribe(lowLevelClient, "a").ConfigureAwait(false);

                await lowLevelClient.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(receivedPacket);
                Assert.AreEqual(MqttSubscribeReasonCode.GrantedQoS0, receivedPacket.ReasonCodes[0]);
            }
        }

        async Task<MqttConnAckPacket> Authenticate(ILowLevelMqttClient client)
        {
            await client.SendAsync(
                    new MqttConnectPacket
                    {
                        CleanSession = true,
                        ClientId = TestContext.TestName,
                        Username = "user",
                        Password = Encoding.UTF8.GetBytes("pass")
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);

            return await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false) as MqttConnAckPacket;
        }

        async Task<MqttSubAckPacket> Subscribe(ILowLevelMqttClient client, string topic)
        {
            await client.SendAsync(
                    new MqttSubscribePacket
                    {
                        PacketIdentifier = 1,
                        TopicFilters =
                        {
                            new MqttTopicFilter
                            {
                                Topic = topic
                            }
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);

            return await client.ReceiveAsync(CancellationToken.None).ConfigureAwait(false) as MqttSubAckPacket;
        }
    }
}