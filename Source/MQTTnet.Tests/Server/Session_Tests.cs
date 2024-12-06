// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Session_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Clean_Session_Persistence()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                // Create server with persistent sessions enabled

                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                const string clientId = "Client1";

                // Create client with clean session and long session expiry interval

                var client1 = await testEnvironment.ConnectClient(
                    o => o.WithProtocolVersion(MqttProtocolVersion.V311)
                        .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                        .WithSessionExpiryInterval(9999) // not relevant for v311 but testing impact
                        .WithCleanSession() // start and end with clean session
                        .WithClientId(clientId)
                        .Build());

                // Disconnect; empty session should be removed from server

                await client1.DisconnectAsync();

                // Simulate some time delay between connections

                await Task.Delay(1000);

                // Reconnect the same client ID without clean session

                var client2 = testEnvironment.CreateClient();
                var options = testEnvironment.ClientFactory.CreateClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTcpServer("127.0.0.1", testEnvironment.ServerPort)
                    .WithSessionExpiryInterval(9999) // not relevant for v311 but testing impact
                    .WithCleanSession(false) // see if there is a session
                    .WithClientId(clientId)
                    .Build();


                var result = await client2.ConnectAsync(options).ConfigureAwait(false);

                await client2.DisconnectAsync();

                // Session should NOT be present for MQTT v311 and initial CleanSession == true

                Assert.IsTrue(!result.IsSessionPresent, "Session present");
            }
        }

        [TestMethod]
        public async Task Do_Not_Use_Expired_Session()
        {
            using var testEnvironments = CreateMixedTestEnvironment(MqttProtocolVersion.V500);
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                var c1 = await testEnvironment.ConnectClient(o => o.WithClientId("Client1").WithCleanSession(false).WithSessionExpiryInterval(3));

                // Kill the client connection and ensure that the session will stay there but is expired after 3 seconds!
                c1.Dispose();

                await Task.Delay(TimeSpan.FromSeconds(6));

                c1 = testEnvironment.CreateClient();
                var options = testEnvironment.CreateDefaultClientOptionsBuilder().WithClientId("Client1").WithCleanSession(false).WithSessionExpiryInterval(3).Build();

                var connectResult = await c1.ConnectAsync(options);
                Assert.AreEqual(false, connectResult.IsSessionPresent);
            }
        }

        [TestMethod]
        public async Task Fire_Deleted_Event()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                // Arrange client and server.
                var server = await testEnvironment.StartServer(o => o.WithPersistentSessions(false));

                var deletedEventFiredTaskSource = new TaskCompletionSource<bool>();
                server.SessionDeletedAsync += e =>
                {
                    deletedEventFiredTaskSource.TrySetResult(true);
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                // Act: Disconnect the client -> Event must be fired.
                await client.DisconnectAsync();

                var deletedEventFired = await deletedEventFiredTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                // Assert that the event was fired properly.
                Assert.IsTrue(deletedEventFired);
            }
        }

        [TestMethod]
        public async Task Get_Session_Items_In_Status()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";

                    return CompletedTask.Instance;
                };

                await testEnvironment.ConnectClient();

                var sessionStatus = await testEnvironment.Server.GetSessionsAsync();
                var session = sessionStatus.First();

                Assert.AreEqual(true, session.Items["can_subscribe_x"]);
            }
        }

        [TestMethod]
        [DataRow(MqttProtocolVersion.V310)]
        [DataRow(MqttProtocolVersion.V311)]
        [DataRow(MqttProtocolVersion.V500)]
        public async Task Handle_Parallel_Connection_Attempts(MqttProtocolVersion protocolVersion)
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer();

                var options = new MqttClientOptionsBuilder().WithClientId("1").WithTimeout(TimeSpan.FromSeconds(10)).WithProtocolVersion(protocolVersion);


                var hasReceiveTaskSource = new TaskCompletionSource<bool>();
                void OnReceive()
                {
                    hasReceiveTaskSource.TrySetResult(true);
                }

                // Try to connect 50 clients at the same time.
                var clients = await Task.WhenAll(Enumerable.Range(0, 50).Select(i => ConnectAndSubscribe(testEnvironment, options, OnReceive)));

                var connectedClients = clients.Where(c => c?.TryPingAsync().GetAwaiter().GetResult() == true).ToList();

                await LongTestDelay();

                Assert.AreEqual(1, connectedClients.Count);

                var option2 = new MqttClientOptionsBuilder().WithClientId("2").WithKeepAlivePeriod(TimeSpan.FromSeconds(10));
                var sendClient = await testEnvironment.ConnectClient(option2);
                await sendClient.PublishStringAsync("aaa", "1");

                var hasReceive = await hasReceiveTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));

                Assert.AreEqual(true, hasReceive);
            }
        }

        [TestMethod]
        [DataRow(MqttQualityOfServiceLevel.ExactlyOnce)]
        [DataRow(MqttQualityOfServiceLevel.AtLeastOnce)]
        public async Task Retry_If_Not_PubAck(MqttQualityOfServiceLevel qos)
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                long count = 0;
                await testEnvironment.StartServer(o => o.WithPersistentSessions());

                var publisher = await testEnvironment.ConnectClient();

                var subscriber = await testEnvironment.ConnectClient(o => o.WithClientId(qos.ToString()).WithCleanSession(false));

                subscriber.ApplicationMessageReceivedAsync += c =>
                {
                    c.AutoAcknowledge = false;
                    ++count;
                    Console.WriteLine("process");
                    return CompletedTask.Instance;
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
            using var testEnvironments = CreateMixedTestEnvironment();           
            foreach (var testEnvironment in testEnvironments)
            {
                await testEnvironment.StartServer();

                var options = new MqttClientOptionsBuilder().WithCleanSession(false)
                    .WithProtocolVersion(MqttProtocolVersion.V500) // Disconnect reason is only available in MQTT 5+
                    .WithClientId("a");

                var client1 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                var disconnectReason = MqttClientDisconnectReason.NormalDisconnection;
                var disconnectTaskSource = new TaskCompletionSource();
                client1.DisconnectedAsync += c =>
                {
                    disconnectReason = c.Reason;
                    disconnectTaskSource.TrySetResult();
                    return CompletedTask.Instance;
                };

                var client2 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                Assert.IsFalse(client1.IsConnected);
                Assert.IsTrue(client2.IsConnected);

                await disconnectTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10d));
                Assert.AreEqual(MqttClientDisconnectReason.SessionTakenOver, disconnectReason);
            }
        }

        [TestMethod]
        public async Task Set_Session_Item()
        {
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var server = await testEnvironment.StartServer();
                server.ValidatingConnectionAsync += e =>
                {
                    // Don't validate anything. Just set some session items.
                    e.SessionItems["can_subscribe_x"] = true;
                    e.SessionItems["default_payload"] = "Hello World";
                    return CompletedTask.Instance;
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

                    return CompletedTask.Instance;
                };

                server.InterceptingPublishAsync += e =>
                {
                    e.ApplicationMessage.PayloadSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(e.SessionItems["default_payload"] as string ?? string.Empty));
                    return CompletedTask.Instance;
                };

                string receivedPayload = null;

                var client = await testEnvironment.ConnectClient();
                client.ApplicationMessageReceivedAsync += e =>
                {
                    receivedPayload = e.ApplicationMessage.ConvertPayloadToString();
                    return CompletedTask.Instance;
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
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
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
            using var testEnvironments = CreateMixedTestEnvironment();
            foreach (var testEnvironment in testEnvironments)
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                // C1 will receive the last will!
                var c1 = await testEnvironment.ConnectClient();
                c1.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return CompletedTask.Instance;
                };

                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                // C2 has the last will defined.
                var clientOptions = new MqttClientOptionsBuilder().WithWillTopic("My/last/will").WithClientId("WillOwner");

                await testEnvironment.ConnectClient(clientOptions);

                // C3 will do the connection takeover.
                await testEnvironment.ConnectClient(clientOptions);

                await LongTestDelay();

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        static async Task<IMqttClient> ConnectAndSubscribe(TestEnvironment testEnvironment, MqttClientOptionsBuilder options, Action onReceive)
        {
            try
            {
                var client = await testEnvironment.ConnectClient(options).ConfigureAwait(false);

                client.ApplicationMessageReceivedAsync += e =>
                {
                    onReceive();
                    return CompletedTask.Instance;
                };

                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    await client.SubscribeAsync("aaa", MqttQualityOfServiceLevel.AtMostOnce, timeout.Token).ConfigureAwait(false);
                }

                return client;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}