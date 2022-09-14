// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Session_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Clean_Session_Persistence()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                // Create server with persistent sessions enabled

                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                const string ClientId = "Client1";

                // Create client with clean session and long session expiry interval

                var client1 = await testEnvironment.ConnectClient(
                    o => o.WithProtocolVersion(MqttProtocolVersion.V311)
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .WithSessionExpiryInterval(9999) // not relevant for v311 but testing impact
                        .WithCleanSession() // start and end with clean session
                        .WithClientId(ClientId)
                        .Build());

                // Disconnect; empty session should be removed from server

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID without clean session

                var client2 = testEnvironment.CreateClient();
                var options = testEnvironment.Factory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithSessionExpiryInterval(9999) // not relevant for v311 but testing impact
                    .WithCleanSession(false) // see if there is a session
                    .WithClientId(ClientId)
                    .Build();


                var result = await client2.ConnectAsync(options).ConfigureAwait(false);

                await client2.DisconnectAsync();

                // Session should NOT be present for MQTT v311 and initial CleanSession == true

                Assert.IsTrue(!result.IsSessionPresent, "Session present");
            }
        }

        [TestMethod]
        public async Task Fire_Deleted_Event()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                // Arrange client and server.
                var server = await testEnvironment.StartServer(o => o.WithPersistentSessions(false));

                var deletedEventFired = false;
                server.SessionDeletedAsync += e =>
                {
                    deletedEventFired = true;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                var client = await testEnvironment.ConnectClient();

                // Act: Disconnect the client -> Event must be fired.
                await client.DisconnectAsync();

                await LongTestDelay();

                // Assert that the event was fired properly.
                Assert.IsTrue(deletedEventFired);
            }
        }

        [TestMethod]
        public async Task Get_Session_Items_In_Status()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";

                    return PlatformAbstractionLayer.CompletedTask;
                };

                await testEnvironment.ConnectClient();

                var sessionStatus = await testEnvironment.Server.GetSessionsAsync();
                var session = sessionStatus.First();

                Assert.AreEqual(true, session.Items["can_subscribe_x"]);
            }
        }

        [TestMethod]
        [DataRow(MqttProtocolVersion.V311)]
        [DataRow(MqttProtocolVersion.V500)]
        public async Task Handle_Parallel_Connection_Attempts(MqttProtocolVersion protocolVersion)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer();

                var options = new MqttClientOptionsBuilder().WithClientId("1").WithTimeout(TimeSpan.FromSeconds(1)).WithProtocolVersion(protocolVersion).WithKeepAlivePeriod(TimeSpan.FromSeconds(5));

                var hasReceive = false;

                void OnReceive()
                {
                    if (!hasReceive)
                    {
                        hasReceive = true;
                    }
                }

                // Try to connect 50 clients at the same time.
                var clients = await Task.WhenAll(Enumerable.Range(0, 50).Select(i => ConnectAndSubscribe(testEnvironment, options, OnReceive)));

                await LongTestDelay();
                
                var connectedClients = clients.Where(c => c != null).Where(c => c.TryPingAsync().GetAwaiter().GetResult()).ToList();

                Assert.AreEqual(1, connectedClients.Count);

                await Task.Delay(5000);

                var option2 = new MqttClientOptionsBuilder().WithClientId("2").WithKeepAlivePeriod(TimeSpan.FromSeconds(10));
                var sendClient = await testEnvironment.ConnectClient(option2);
                await sendClient.PublishStringAsync("aaa", "1");

                await Task.Delay(3000);

                Assert.AreEqual(true, hasReceive);
            }
        }
        
        [DataTestMethod]
        [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
        [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
        public async Task Retry_If_Not_PubAck(MqttQualityOfServiceLevel qos)
        {
            long count = 0;
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                var publisher = await testEnvironment.ConnectClient();

                var subscriber = await testEnvironment.ConnectClient(o => o.WithClientId(qos.ToString()).WithCleanSession(false));

                subscriber.ApplicationMessageReceivedAsync += c =>
                {
                    c.AutoAcknowledge = false;
                    ++count;
                    Console.WriteLine("process");
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await subscriber.SubscribeAsync("#", qos);

                var pub = publisher.PublishStringAsync("a", null, qos);

                await Task.Delay(100);
                await subscriber.DisconnectAsync();
                await subscriber.ConnectAsync(subscriber.Options);
                await Task.Delay(100);

                var res = await pub;

                Assert.AreEqual(MqttClientPublishReasonCode.Success, res.ReasonCode);
                Assert.AreEqual(2, count);
            }
        }

        [TestMethod]
        public async Task Session_Takeover()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var options = new MqttClientOptionsBuilder().WithCleanSession(false)
                    .WithProtocolVersion(MqttProtocolVersion.V500) // Disconnect reason is only available in MQTT 5+
                    .WithClientId("a");

                var client1 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                var disconnectReason = MqttClientDisconnectReason.NormalDisconnection;
                client1.DisconnectedAsync += c =>
                {
                    disconnectReason = c.Reason;
                    return PlatformAbstractionLayer.CompletedTask;
                };

                var client2 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                Assert.IsFalse(client1.IsConnected);
                Assert.IsTrue(client2.IsConnected);

                Assert.AreEqual(MqttClientDisconnectReason.SessionTakenOver, disconnectReason);
            }
        }

        [TestMethod]
        public async Task Set_Session_Item()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";

                    return PlatformAbstractionLayer.CompletedTask;
                };

                server.InterceptingSubscriptionAsync += e =>
                {
                    if (e.TopicFilter.Topic == "x")
                    {
                        if (e.SessionItems["can_subscribe_x"] as bool? == false)
                        {
                            e.Response.ReasonCode = MqttSubscribeReasonCode.ImplementationSpecificError;
                        }
                    }

                    return PlatformAbstractionLayer.CompletedTask;
                };

                server.InterceptingPublishAsync += e =>
                {
                    e.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(e.SessionItems["default_payload"] as string ?? string.Empty);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                string receivedPayload = null;

                var client = await testEnvironment.ConnectClient();
                client.ApplicationMessageReceivedAsync += e =>
                {
                    receivedPayload = e.ApplicationMessage.ConvertPayloadToString();
                    return PlatformAbstractionLayer.CompletedTask;
                };

                var subscribeResult = await client.SubscribeAsync("x");

                Assert.AreEqual(MqttClientSubscribeResultCode.GrantedQoS0, subscribeResult.Items.First().ResultCode);

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishStringAsync("x");

                await Task.Delay(1000);

                Assert.AreEqual("Hello World", receivedPayload);
            }
        }

        [TestMethod]
        public async Task Use_Clean_Session()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).WithCleanSession().Build());

                // Create the session including the subscription.
                var client1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("a").WithCleanSession(false));
                await client1.SubscribeAsync("x");
                await client1.DisconnectAsync();
                await Task.Delay(500);

                Assert.IsFalse(connectResult.IsSessionPresent);
            }
        }

        [TestMethod]
        public async Task Will_Message_Do_Not_Send_On_Takeover()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                // C1 will receive the last will!
                var c1 = await testEnvironment.ConnectClient();
                c1.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return PlatformAbstractionLayer.CompletedTask;
                };

                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                // C2 has the last will defined.
                var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithClientId("WillOwner");

                var c2 = await testEnvironment.ConnectClient(clientOptions);

                // C3 will do the connection takeover.
                var c3 = await testEnvironment.ConnectClient(clientOptions);

                await Task.Delay(1000);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        static async Task<IMqttClient> ConnectAndSubscribe(TestEnvironment testEnvironment, MqttClientOptionsBuilder options, Action onReceive)
        {
            try
            {
                var sendClient = await testEnvironment.ConnectClient(options);

                sendClient.ApplicationMessageReceivedAsync += e =>
                {
                    onReceive();
                    return PlatformAbstractionLayer.CompletedTask;
                };

                using (var subscribeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    await sendClient.SubscribeAsync("aaa", MqttQualityOfServiceLevel.AtMostOnce, subscribeTimeout.Token).ConfigureAwait(false);
                }

                return sendClient;
            }
            catch
            {
                return null;
            }
        }

        static async Task<IMqttClient> TryConnect(TestEnvironment testEnvironment, MqttClientOptionsBuilder options)
        {
            try
            {
                return await testEnvironment.ConnectClient(options);
            }
            catch
            {
                return null;
            }
        }
    }
}