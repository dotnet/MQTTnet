// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class General_Tests : BaseTestClass
    {
        Dictionary<string, bool> _connected;

        [TestMethod]
        public async Task Client_Disconnect_Without_Errors()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                bool clientWasConnected;

                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder());
                try
                {
                    var client = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder());

                    clientWasConnected = true;

                    await client.DisconnectAsync();

                    await Task.Delay(500);
                }
                finally
                {
                    await server.StopAsync();
                }

                Assert.IsTrue(clientWasConnected);

                testEnvironment.ThrowIfLogErrors();
            }
        }

        [TestMethod]
        public async Task Collect_Messages_In_Disconnected_Session()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithPersistentSessions());

                // Create the session including the subscription.
                var client1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("a").WithCleanSession(false).WithSessionExpiryInterval(60));
                await client1.SubscribeAsync("x");
                await client1.DisconnectAsync();
                await Task.Delay(500);

                var clientStatus = await server.GetClientsAsync();
                Assert.AreEqual(0, clientStatus.Count);

                var client2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("b").WithCleanSession(false).WithSessionExpiryInterval(60));
                await client2.PublishStringAsync("x", "1");
                await client2.PublishStringAsync("x", "2");
                await client2.PublishStringAsync("x", "3");
                await client2.DisconnectAsync();

                await Task.Delay(500);

                clientStatus = await server.GetClientsAsync();
                var sessionStatus = await server.GetSessionsAsync();

                Assert.AreEqual(0, clientStatus.Count);
                Assert.AreEqual(2, sessionStatus.Count);

                Assert.AreEqual(3, sessionStatus.First(s => s.Id == client1.Options.ClientId).PendingApplicationMessagesCount);
            }
        }

        [TestMethod]
        public async Task Deny_Connection()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    e.ReasonCode = MqttConnectReasonCode.NotAuthorized;
                    return CompletedTask.Instance;
                };

                var client = testEnvironment.CreateClient();
                var response = await client.ConnectAsync(testEnvironment.CreateDefaultClientOptions());

                Assert.AreEqual(MqttClientConnectResultCode.NotAuthorized, response.ResultCode);
            }
        }

        [TestMethod]
        public async Task Do_Not_Send_Retained_Messages_For_Denied_Subscription()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.InterceptingSubscriptionAsync += e =>
                {
                    // This should lead to no subscriptions for "n" at all. So also no sending of retained messages.
                    if (e.TopicFilter.Topic == "n")
                    {
                        e.Response.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                    }

                    return CompletedTask.Instance;
                };

                // Prepare some retained messages.
                var client1 = await testEnvironment.ConnectClient();
                await client1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("y").WithPayload("x").WithRetainFlag().Build());
                await client1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("n").WithPayload("x").WithRetainFlag().Build());
                await client1.DisconnectAsync();

                await Task.Delay(500);

                // Subscribe to all retained message types.
                // It is important to do this in a range of filters to ensure that a subscription is not "hidden".
                var client2 = await testEnvironment.ConnectClient();

                var buffer = new StringBuilder();

                client2.ApplicationMessageReceivedAsync += e =>
                {
                    lock (buffer)
                    {
                        buffer.Append(e.ApplicationMessage.Topic);
                    }

                    return CompletedTask.Instance;
                };

                await client2.SubscribeAsync("y");
                await client2.SubscribeAsync("n");

                await Task.Delay(500);

                // No 'n' in buffer!
                Assert.AreEqual("y", buffer.ToString());
            }
        }

        [TestMethod]
        public async Task Handle_Clean_Disconnect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder());

                var clientConnectedCalled = 0;
                var clientDisconnectedCalled = 0;

                server.ClientConnectedAsync += e =>
                {
                    Interlocked.Increment(ref clientConnectedCalled);
                    return CompletedTask.Instance;
                };

                server.ClientDisconnectedAsync += e =>
                {
                    Interlocked.Increment(ref clientDisconnectedCalled);
                    return CompletedTask.Instance;
                };

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder());

                await Task.Delay(1000);

                Assert.AreEqual(1, clientConnectedCalled);
                Assert.AreEqual(0, clientDisconnectedCalled);

                await Task.Delay(1000);

                await c1.DisconnectAsync();

                await Task.Delay(1000);

                Assert.AreEqual(1, clientConnectedCalled);
                Assert.AreEqual(1, clientDisconnectedCalled);
            }
        }

        [TestMethod]
        public async Task Handle_Lots_Of_Parallel_Retained_Messages()
        {
            const int clientCount = 50;

            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var tasks = new List<Task>();
                for (var i = 0; i < clientCount; i++)
                {
                    var i2 = i;
                    var testEnvironment2 = testEnvironment;

                    tasks.Add(
                        Task.Run(
                            async () =>
                            {
                                try
                                {
                                    using (var client = await testEnvironment2.ConnectClient())
                                    {
                                        // Clear retained message.
                                        await client.PublishAsync(
                                            new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                                .WithPayload(EmptyBuffer.Array)
                                                .WithRetainFlag()
                                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                                .Build());

                                        // Set retained message.
                                        await client.PublishAsync(
                                            new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                                .WithPayload("value")
                                                .WithRetainFlag()
                                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                                .Build());

                                        await client.DisconnectAsync();
                                    }
                                }
                                catch (Exception exception)
                                {
                                    testEnvironment2.TrackException(exception);
                                }
                            }));

                    await Task.Delay(10);
                }

                await Task.WhenAll(tasks);

                await Task.Delay(1000);

                var retainedMessages = await server.GetRetainedMessagesAsync();

                Assert.AreEqual(clientCount, retainedMessages.Count);

                for (var i = 0; i < clientCount; i++)
                {
                    Assert.IsTrue(retainedMessages.Any(m => m.Topic == "r" + i));
                }
            }
        }

        [TestMethod]
        public async Task Intercept_Application_Message()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.InterceptingPublishAsync += e =>
                {
                    e.ApplicationMessage = new MqttApplicationMessage { Topic = "new_topic" };

                    return CompletedTask.Instance;
                };

                string receivedTopic = null;
                var c1 = await testEnvironment.ConnectClient();
                await c1.SubscribeAsync("#");

                c1.ApplicationMessageReceivedAsync += a =>
                {
                    receivedTopic = a.ApplicationMessage.Topic;
                    return CompletedTask.Instance;
                };

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("original_topic").Build());

                await Task.Delay(500);
                Assert.AreEqual("new_topic", receivedTopic);
            }
        }

        [TestMethod]
        public async Task Intercept_Message()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
                server.InterceptingPublishAsync += e =>
                {
                    e.ApplicationMessage.Payload = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("extended"));
                    return CompletedTask.Instance;
                };

                var c1 = await testEnvironment.ConnectClient();
                var c2 = await testEnvironment.ConnectClient();
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("test").Build());

                var isIntercepted = false;
                c2.ApplicationMessageReceivedAsync += e =>
                {
                    isIntercepted = string.Compare("extended", Encoding.UTF8.GetString(e.ApplicationMessage.Payload), StringComparison.Ordinal) == 0;
                    return CompletedTask.Instance;
                };

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("test").Build());
                await c1.DisconnectAsync();

                await Task.Delay(500);

                Assert.IsTrue(isIntercepted);
            }
        }

        [TestMethod]
        public async Task Intercept_Undelivered()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var undeliverd = string.Empty;

                var server = await testEnvironment.StartServer();
                server.ApplicationMessageNotConsumedAsync += e =>
                {
                    undeliverd = e.ApplicationMessage.Topic;
                    return CompletedTask.Instance;
                };

                var client = await testEnvironment.ConnectClient();

                await client.SubscribeAsync("b");

                await client.PublishStringAsync("a", null, MqttQualityOfServiceLevel.ExactlyOnce);

                await Task.Delay(500);

                Assert.AreEqual("a", undeliverd);
            }
        }

        [TestMethod]
        public async Task No_Messages_If_No_Subscription()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = await testEnvironment.ConnectClient();
                var receivedMessages = new List<MqttApplicationMessage>();

                client.ConnectedAsync += async e =>
                {
                    await client.PublishStringAsync("Connected");
                };

                client.ApplicationMessageReceivedAsync += e =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }

                    return CompletedTask.Instance;
                };

                await Task.Delay(500);

                await client.PublishStringAsync("Hello");

                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessages.Count);
            }
        }

        [TestMethod]
        public async Task Persist_Retained_Message()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                List<MqttApplicationMessage> savedRetainedMessages = null;

                var s = await testEnvironment.StartServer();
                s.RetainedMessageChangedAsync += e =>
                {
                    savedRetainedMessages = e.StoredRetainedMessages;
                    return CompletedTask.Instance;
                };

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());

                await Task.Delay(500);

                Assert.AreEqual(1, savedRetainedMessages?.Count);
            }
        }

        [TestMethod]
        public async Task Publish_After_Client_Connects()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
                server.ClientConnectedAsync += async e =>
                {
                    await server.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(
                            new MqttApplicationMessage
                            {
                                Topic = "/test/1",
                                Payload = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("true")),
                                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
                            })
                        {
                            SenderClientId = "server"
                        });
                };

                string receivedTopic = null;

                var c1 = await testEnvironment.ConnectClient();
                c1.ApplicationMessageReceivedAsync += e =>
                {
                    receivedTopic = e.ApplicationMessage.Topic;
                    return CompletedTask.Instance;
                };

                await c1.SubscribeAsync("#");

                await testEnvironment.ConnectClient();
                await testEnvironment.ConnectClient();
                await testEnvironment.ConnectClient();
                await testEnvironment.ConnectClient();

                await Task.Delay(500);

                Assert.AreEqual("/test/1", receivedTopic);
            }
        }

        [TestMethod]
        public async Task Publish_At_Least_Once_0x01()
        {
            await TestPublishAsync("A/B/C", MqttQualityOfServiceLevel.AtLeastOnce, "A/B/C", MqttQualityOfServiceLevel.AtLeastOnce, 1);
        }

        [TestMethod]
        public async Task Publish_At_Most_Once_0x00()
        {
            await TestPublishAsync("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "A/B/C", MqttQualityOfServiceLevel.AtMostOnce, 1);
        }

        [TestMethod]
        public async Task Publish_Exactly_Once_0x02()
        {
            await TestPublishAsync("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, 1);
        }

        [TestMethod]
        public async Task Publish_From_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var receivedMessagesCount = 0;

                var client = await testEnvironment.ConnectClient();
                client.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return CompletedTask.Instance;
                };

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();
                await client.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

                await server.InjectApplicationMessage(
                    new InjectedMqttApplicationMessage(message)
                    {
                        SenderClientId = "server"
                    });

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Publish_Multiple_Clients()
        {
            var receivedMessagesCount = 0;

            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient();
                var c2 = await testEnvironment.ConnectClient();
                var c3 = await testEnvironment.ConnectClient();

                c2.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return CompletedTask.Instance;
                };

                c3.ApplicationMessageReceivedAsync += e =>
                {
                    Interlocked.Increment(ref receivedMessagesCount);
                    return CompletedTask.Instance;
                };

                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);
                await c3.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build();

                for (var i = 0; i < 500; i++)
                {
                    await c1.PublishAsync(message).ConfigureAwait(false);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 1000, TimeSpan.FromSeconds(20));

                Assert.AreEqual(1000, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Remove_Session()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder());

                var clientOptions = new MqttClientOptionsBuilder();
                var c1 = await testEnvironment.ConnectClient(clientOptions);
                await Task.Delay(500);
                Assert.AreEqual(1, (await server.GetClientsAsync()).Count);

                await c1.DisconnectAsync();
                await Task.Delay(500);

                Assert.AreEqual(0, (await server.GetClientsAsync()).Count);
            }
        }

        [TestMethod]
        public async Task Same_Client_Id_Connect_Disconnect_Event_Order()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var events = new List<string>();

                server.ClientConnectedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("c");
                    }

                    return CompletedTask.Instance;
                };

                server.ClientDisconnectedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("d");
                    }

                    return CompletedTask.Instance;
                };

                var clientOptionsBuilder = new MqttClientOptionsBuilder().WithClientId(Guid.NewGuid().ToString());

                // c
                var c1 = await testEnvironment.ConnectClient(clientOptionsBuilder);

                await LongTestDelay();
                var flow = string.Join(string.Empty, events);
                Assert.AreEqual("c", flow);

                // dc
                // Connect client with same client ID. Should disconnect existing client.
                var c2 = await testEnvironment.ConnectClient(clientOptionsBuilder);

                await LongTestDelay();
                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdc", flow);

                c2.ApplicationMessageReceivedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("r");
                    }

                    return CompletedTask.Instance;
                };

                await c2.SubscribeAsync("topic");

                // r
                await c2.PublishStringAsync("topic");

                await LongTestDelay();
                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdcr", flow);

                // nothing

                Assert.AreEqual(false, c1.IsConnected);
                await c1.TryDisconnectAsync();
                Assert.AreEqual(false, c1.IsConnected);

                await LongTestDelay();

                // d
                Assert.AreEqual(true, c2.IsConnected);
                await c2.DisconnectAsync();

                await LongTestDelay();

                await server.StopAsync();

                await LongTestDelay();
                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdcrd", flow);
            }
        }

        [TestMethod]
        public async Task Same_Client_Id_Refuse_Connection()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                _connected = new Dictionary<string, bool>();

                var server = await testEnvironment.StartServer();

                server.ValidatingConnectionAsync += e =>
                {
                    ConnectionValidationHandler(e);
                    return CompletedTask.Instance;
                };

                var events = new List<string>();

                server.ClientConnectedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("c");
                    }

                    return CompletedTask.Instance;
                };

                server.ClientDisconnectedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("d");
                    }

                    return CompletedTask.Instance;
                };

                var clientOptions = new MqttClientOptionsBuilder().WithClientId("same_id");

                // c
                var c1 = await testEnvironment.ConnectClient(clientOptions);

                c1.DisconnectedAsync += _ =>
                {
                    lock (events)
                    {
                        events.Add("x");
                    }

                    return CompletedTask.Instance;
                };

                c1.ApplicationMessageReceivedAsync += e =>
                {
                    lock (events)
                    {
                        events.Add("r");
                    }

                    return CompletedTask.Instance;
                };

                c1.SubscribeAsync("topic").Wait();

                await Task.Delay(500);

                c1.PublishStringAsync("topic").Wait();

                await Task.Delay(500);

                var flow = string.Join(string.Empty, events);
                Assert.AreEqual("cr", flow);

                try
                {
                    await testEnvironment.ConnectClient(clientOptions);
                    Assert.Fail("same id connection is expected to fail");
                }
                catch
                {
                    //same id connection is expected to fail
                }

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cr", flow);

                c1.PublishStringAsync("topic").Wait();

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("crr", flow);
            }
        }

        [TestMethod]
        public async Task Send_Long_Body()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                const int PayloadSizeInMB = 30;
                const int CharCount = PayloadSizeInMB * 1024 * 1024;

                var longBody = new byte[CharCount];
                byte @char = 32;

                for (long i = 0; i < PayloadSizeInMB * 1024L * 1024L; i++)
                {
                    longBody[i] = @char;

                    @char++;

                    if (@char > 126)
                    {
                        @char = 32;
                    }
                }

                byte[] receivedBody = null;

                await testEnvironment.StartServer();

                var client1 = await testEnvironment.ConnectClient();
                client1.ApplicationMessageReceivedAsync += e =>
                {
                    receivedBody = e.ApplicationMessage.Payload.ToArray();
                    return CompletedTask.Instance;
                };

                await client1.SubscribeAsync("string");

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishBinaryAsync("string", longBody);

                await Task.Delay(TimeSpan.FromSeconds(5));

                Assert.IsTrue(longBody.SequenceEqual(receivedBody ?? new byte[0]));
            }
        }

        [TestMethod]
        public async Task Set_Subscription_At_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ClientConnectedAsync += async e =>
                {
                    // Every client will automatically subscribe to this topic.
                    await server.SubscribeAsync(e.ClientId, "topic1");
                };

                var client = await testEnvironment.ConnectClient();
                var receivedMessages = new List<MqttApplicationMessage>();

                client.ApplicationMessageReceivedAsync += e =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }

                    return CompletedTask.Instance;
                };

                await Task.Delay(500);

                await client.PublishStringAsync("Hello");
                await Task.Delay(100);
                Assert.AreEqual(0, receivedMessages.Count);

                await client.PublishStringAsync("topic1");
                await Task.Delay(100);
                Assert.AreEqual(1, receivedMessages.Count);
            }
        }

        [TestMethod]
        public async Task Shutdown_Disconnects_Clients_Gracefully()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder());

                var disconnectCalled = 0;

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder());
                c1.DisconnectedAsync += e =>
                {
                    disconnectCalled++;
                    return CompletedTask.Instance;
                };

                // Stopping the server should disconnect the connection with the client
                // which, it turn, will fire the disconnected event at the client.
                await server.StopAsync();

                await LongTestDelay();

                Assert.AreEqual(1, disconnectCalled);
            }
        }

        [TestMethod]
        public async Task Stop_And_Restart()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var server = await testEnvironment.StartServer();

                await testEnvironment.ConnectClient();
                await server.StopAsync();

                try
                {
                    await testEnvironment.ConnectClient();
                    Assert.Fail("Connecting should fail.");
                }
                catch
                {
                    // Ignore errors.
                }

                await server.StartAsync();

                // The client should be able to connect again.
                await testEnvironment.ConnectClient();
            }
        }

        [TestMethod]
        [DataRow("", null)]
        [DataRow("", "")]
        [DataRow(null, null)]
        public async Task Use_Admissible_Credentials(string username, string password)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).WithCredentials(username, password).Build();

                var connectResult = await client.ConnectAsync(clientOptions);

                Assert.IsFalse(connectResult.IsSessionPresent);
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Use_Empty_Client_ID()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();
                var client2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("b").WithCleanSession(false));
                await client2.PublishStringAsync("x", "1");
                await client2.PublishStringAsync("x", "2");
                await client2.PublishStringAsync("x", "3");
                await client2.DisconnectAsync();

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).WithClientId(string.Empty).Build();

                var connectResult = await client.ConnectAsync(clientOptions);

                Assert.IsFalse(connectResult.IsSessionPresent);
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Disconnect_Client_with_Reason()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var disconnectPacketReceived = false;

                string testClientId = null;

                await testEnvironment.StartServer();

                testEnvironment.Server.ClientConnectedAsync += e =>
                {
                    testClientId = e.ClientId;
                    return CompletedTask.Instance;
                };

                var client = testEnvironment.CreateClient();

                client.InspectPacketAsync += e =>
                {
                    if (e.Buffer.Length > 0)
                    {
                        if (e.Buffer[0] == (byte)MqttControlPacketType.Disconnect << 4)
                        {
                            disconnectPacketReceived = true;
                        }
                    }
                    return CompletedTask.Instance;
                };

                await client.ConnectAsync(
                    new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort)
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                    .Build()
                    );

                await LongTestDelay();

                // Test client should be connected now

                Assert.IsTrue(testClientId != null);

                // Have the server disconnect the client with SessionTakenOver reason; no DISCONNECT should be sent to MQTT 3.1.1 clients

                await testEnvironment.Server.DisconnectClientAsync(testClientId, Protocol.MqttDisconnectReasonCode.SessionTakenOver);

                await LongTestDelay();

                // MQTT 3.1.1 client should not receive a disconnect packet

                Assert.IsFalse(disconnectPacketReceived, "Disconnect packet received when none is expected");
            }
        }

        void ConnectionValidationHandler(ValidatingConnectionEventArgs eventArgs)
        {
            if (_connected.ContainsKey(eventArgs.ClientId))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            _connected[eventArgs.ClientId] = true;
            eventArgs.ReasonCode = MqttConnectReasonCode.Success;
        }

        async Task TestPublishAsync(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            string topicFilter,
            MqttQualityOfServiceLevel filterQualityOfServiceLevel,
            int expectedReceivedMessagesCount)
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var c1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("receiver"));
                using var c1MessageHandler = testEnvironment.CreateApplicationMessageHandler(c1);
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());

                var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("sender"));
                await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
                await Task.Delay(500);

                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(500);
                await c1.UnsubscribeAsync(topicFilter);
                await Task.Delay(500);

                Assert.AreEqual(expectedReceivedMessagesCount, c1MessageHandler.ReceivedEventArgs.Count);
            }
        }
    }
}