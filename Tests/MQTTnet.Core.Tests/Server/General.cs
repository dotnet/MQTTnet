using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class General_Tests : BaseTestClass
    {
        [TestMethod]
        [DataRow("", null)]
        [DataRow("", "")]
        [DataRow(null, null)]
        public async Task Use_Admissible_Credentials(string username, string password)
        {
            using (var testEnvironment = new TestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort)
                    .WithCredentials(username, password)
                    .Build();

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

                var client = testEnvironment.CreateClient();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", testEnvironment.ServerPort)
                    .WithClientId(string.Empty)
                    .Build();

                var connectResult = await client.ConnectAsync(clientOptions);

                Assert.IsFalse(connectResult.IsSessionPresent);
                Assert.IsTrue(client.IsConnected);
            }
        }

        [TestMethod]
        public async Task Publish_At_Most_Once_0x00()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                1);
        }

        [TestMethod]
        public async Task Publish_At_Least_Once_0x01()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                1);
        }

        [TestMethod]
        public async Task Publish_Exactly_Once_0x02()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                1);
        }

        [TestMethod]
        public async Task Use_Clean_Session()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).WithCleanSession().Build());

                Assert.IsFalse(connectResult.IsSessionPresent);
            }
        }

        [TestMethod]
        public async Task Will_Message_Do_Not_Send_On_Clean_Disconnect()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage);

                var c1 = await testEnvironment.ConnectClient();
                c1.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClient(clientOptions);
                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(1000);

                Assert.AreEqual(0, receivedMessagesCount);
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
                c1.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                // C2 has the last will defined.
                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").Build();

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithWillMessage(willMessage)
                    .WithClientId("WillOwner");

                var c2 = await testEnvironment.ConnectClient(clientOptions);

                // C3 will do the connection takeover.
                var c3 = await testEnvironment.ConnectClient(clientOptions);

                await Task.Delay(1000);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServer();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage);

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClient(clientOptions);
                c2.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }
     
        [TestMethod]
        public async Task Publish_From_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var receivedMessagesCount = 0;

                var client = await testEnvironment.ConnectClient();
                client.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
                await client.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

                await server.PublishAsync(message);

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

                c2.UseApplicationMessageReceivedHandler(c => { Interlocked.Increment(ref receivedMessagesCount); });

                c3.UseApplicationMessageReceivedHandler(c => { Interlocked.Increment(ref receivedMessagesCount); });

                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);
                await c3.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();

                for (var i = 0; i < 500; i++)
                {
                    await c1.PublishAsync(message).ConfigureAwait(false);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 1000, TimeSpan.FromSeconds(20));

                Assert.AreEqual(1000, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Session_Takeover()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var options = new MqttClientOptionsBuilder()
                    .WithCleanSession(false)
                    .WithProtocolVersion(MqttProtocolVersion.V500) // Disconnect reason is only available in MQTT 5+
                    .WithClientId("a");

                var client1 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                MqttClientDisconnectReason disconnectReason = MqttClientDisconnectReason.NormalDisconnection;
                client1.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(c => { disconnectReason = c.Reason; });

                var client2 = await testEnvironment.ConnectClient(options);
                await Task.Delay(500);

                Assert.IsFalse(client1.IsConnected);
                Assert.IsTrue(client2.IsConnected);

                Assert.AreEqual(MqttClientDisconnectReason.SessionTakenOver, disconnectReason);
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

                client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e => { await client.PublishAsync("Connected"); });

                client.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(c.ApplicationMessage);
                    }
                });

                await Task.Delay(500);

                await client.PublishAsync("Hello");

                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessages.Count);
            }
        }

        [TestMethod]
        public async Task Set_Subscription_At_Server()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(async e =>
                {
                    // Every client will automatically subscribe to this topic.
                    await server.SubscribeAsync(e.ClientId, "topic1");
                });

                var client = await testEnvironment.ConnectClient();
                var receivedMessages = new List<MqttApplicationMessage>();

                client.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(c.ApplicationMessage);
                    }
                });

                await Task.Delay(500);

                await client.PublishAsync("Hello");
                await Task.Delay(100);
                Assert.AreEqual(0, receivedMessages.Count);

                await client.PublishAsync("topic1");
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
                c1.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(e => disconnectCalled++);

                await Task.Delay(100);

                await server.StopAsync();

                await Task.Delay(100);

                Assert.AreEqual(1, disconnectCalled);
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

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(_ => Interlocked.Increment(ref clientConnectedCalled));
                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(_ => Interlocked.Increment(ref clientDisconnectedCalled));

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
        public async Task Handle_Lots_Of_Parallel_Retained_Messages()
        {
            const int ClientCount = 50;

            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var tasks = new List<Task>();
                for (var i = 0; i < ClientCount; i++)
                {
                    var i2 = i;
                    var testEnvironment2 = testEnvironment;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using (var client = await testEnvironment2.ConnectClient())
                            {
                                // Clear retained message.
                                await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                    .WithPayload(new byte[0]).WithRetainFlag().WithQualityOfServiceLevel(1).Build());

                                // Set retained message.
                                await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                    .WithPayload("value").WithRetainFlag().WithQualityOfServiceLevel(1).Build());

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

                var retainedMessages = await server.GetRetainedApplicationMessagesAsync();

                Assert.AreEqual(ClientCount, retainedMessages.Count);

                for (var i = 0; i < ClientCount; i++)
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
                await testEnvironment.StartServer(
                    new MqttServerOptionsBuilder().WithApplicationMessageInterceptor(
                        c => { c.ApplicationMessage = new MqttApplicationMessage { Topic = "new_topic" }; }));

                string receivedTopic = null;
                var c1 = await testEnvironment.ConnectClient();
                await c1.SubscribeAsync("#");
                c1.UseApplicationMessageReceivedHandler(a => { receivedTopic = a.ApplicationMessage.Topic; });

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("original_topic").Build());

                await Task.Delay(500);
                Assert.AreEqual("new_topic", receivedTopic);
            }
        }

        [TestMethod]
        public async Task Persist_Retained_Message()
        {
            var serverStorage = new TestServerStorage();

            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithStorage(serverStorage));

                var c1 = await testEnvironment.ConnectClient();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());

                await Task.Delay(500);

                Assert.AreEqual(1, serverStorage.Messages.Count);
            }
        }

        [TestMethod]
        public async Task Publish_After_Client_Connects()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
                server.UseClientConnectedHandler(async e => { await server.PublishAsync("/test/1", "true", MqttQualityOfServiceLevel.ExactlyOnce, false); });

                string receivedTopic = null;

                var c1 = await testEnvironment.ConnectClient();
                c1.UseApplicationMessageReceivedHandler(e => { receivedTopic = e.ApplicationMessage.Topic; });
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
        public async Task Intercept_Message()
        {
            void Interceptor(MqttApplicationMessageInterceptorContext context)
            {
                context.ApplicationMessage.Payload = Encoding.ASCII.GetBytes("extended");
            }

            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithApplicationMessageInterceptor(Interceptor));

                var c1 = await testEnvironment.ConnectClient();
                var c2 = await testEnvironment.ConnectClient();
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("test").Build());

                var isIntercepted = false;
                c2.UseApplicationMessageReceivedHandler(c => { isIntercepted = string.Compare("extended", Encoding.UTF8.GetString(c.ApplicationMessage.Payload), StringComparison.Ordinal) == 0; });

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("test").Build());
                await c1.DisconnectAsync();

                await Task.Delay(500);

                Assert.IsTrue(isIntercepted);
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
                client1.UseApplicationMessageReceivedHandler(c => { receivedBody = c.ApplicationMessage.Payload; });

                await client1.SubscribeAsync("string");

                var client2 = await testEnvironment.ConnectClient();
                await client2.PublishAsync("string", longBody);

                await Task.Delay(TimeSpan.FromSeconds(5));

                Assert.IsTrue(longBody.SequenceEqual(receivedBody ?? new byte[0]));
            }
        }

        [TestMethod]
        public async Task Deny_Connection()
        {
            var serverOptions = new MqttServerOptionsBuilder().WithConnectionValidator(context => { context.ReasonCode = MqttConnectReasonCode.NotAuthorized; });

            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServer(serverOptions);

                var connectingFailedException = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(() => testEnvironment.ConnectClient());
                Assert.AreEqual(MqttClientConnectResultCode.NotAuthorized, connectingFailedException.ResultCode);
            }
        }

        Dictionary<string, bool> _connected;

        private void ConnectionValidationHandler(MqttConnectionValidatorContext eventArgs)
        {
            if (_connected.ContainsKey(eventArgs.ClientId))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }

            _connected[eventArgs.ClientId] = true;
            eventArgs.ReasonCode = MqttConnectReasonCode.Success;
            return;
        }

        [TestMethod]
        public async Task Same_Client_Id_Refuse_Connection()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                testEnvironment.IgnoreClientLogErrors = true;

                _connected = new Dictionary<string, bool>();
                var options = new MqttServerOptionsBuilder();
                options.WithConnectionValidator(e => ConnectionValidationHandler(e));
                var server = await testEnvironment.StartServer(options);

                var events = new List<string>();

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(_ =>
                {
                    lock (events)
                    {
                        events.Add("c");
                    }
                });

                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(_ =>
                {
                    lock (events)
                    {
                        events.Add("d");
                    }
                });

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("same_id");

                // c
                var c1 = await testEnvironment.ConnectClient(clientOptions);

                c1.UseDisconnectedHandler(_ =>
                {
                    lock (events)
                    {
                        events.Add("x");
                    }
                });


                c1.UseApplicationMessageReceivedHandler(_ =>
                {
                    lock (events)
                    {
                        events.Add("r");
                    }
                });

                c1.SubscribeAsync("topic").Wait();

                await Task.Delay(500);

                c1.PublishAsync("topic").Wait();

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

                c1.PublishAsync("topic").Wait();

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("crr", flow);
            }
        }

        [TestMethod]
        public async Task Same_Client_Id_Connect_Disconnect_Event_Order()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder());

                var events = new List<string>();

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(_ =>
                {
                    lock (events)
                    {
                        events.Add("c");
                    }
                });

                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(_ =>
                {
                    lock (events)
                    {
                        events.Add("d");
                    }
                });

                var clientOptionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(Guid.NewGuid().ToString());

                // c
                var c1 = await testEnvironment.ConnectClient(clientOptionsBuilder);

                await Task.Delay(500);

                var flow = string.Join(string.Empty, events);
                Assert.AreEqual("c", flow);

                // dc
                // Connect client with same client ID. Should disconnect existing client.
                var c2 = await testEnvironment.ConnectClient(clientOptionsBuilder);

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);

                Assert.AreEqual("cdc", flow);

                c2.UseApplicationMessageReceivedHandler(_ =>
                {
                    lock (events)
                    {
                        events.Add("r");
                    }
                });

                await c2.SubscribeAsync("topic");

                // r
                await c2.PublishAsync("topic");

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdcr", flow);

                // nothing

                Assert.AreEqual(false, c1.IsConnected);
                await c1.DisconnectAsync();
                Assert.AreEqual(false, c1.IsConnected);

                await Task.Delay(500);

                // d
                Assert.AreEqual(true, c2.IsConnected);
                await c2.DisconnectAsync();

                await Task.Delay(500);

                await server.StopAsync();

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdcrd", flow);
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
                Assert.AreEqual(1, (await server.GetClientStatusAsync()).Count);

                await c1.DisconnectAsync();
                await Task.Delay(500);

                Assert.AreEqual(0, (await server.GetClientStatusAsync()).Count);
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
                catch (Exception)
                {
                }

                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultEndpointPort(testEnvironment.ServerPort).Build());
                await testEnvironment.ConnectClient();
            }
        }

        [TestMethod]
        public async Task Do_Not_Send_Retained_Messages_For_Denied_Subscription()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithSubscriptionInterceptor(c =>
                {
                    // This should lead to no subscriptions for "n" at all. So also no sending of retained messages.
                    if (c.TopicFilter.Topic == "n")
                    {
                        c.ReasonCode = MqttSubscribeReasonCode.UnspecifiedError;
                    }
                }));

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

                client2.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (buffer)
                    {
                        buffer.Append(c.ApplicationMessage.Topic);
                    }
                });

                await client2.SubscribeAsync(new MqttTopicFilter { Topic = "y" }, new MqttTopicFilter { Topic = "n" });

                await Task.Delay(500);

                Assert.AreEqual("y", buffer.ToString());
            }
        }

        [TestMethod]
        public async Task Collect_Messages_In_Disconnected_Session()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer(new MqttServerOptionsBuilder().WithPersistentSessions());

                // Create the session including the subscription.
                var client1 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("a"));
                await client1.SubscribeAsync("x");
                await client1.DisconnectAsync();
                await Task.Delay(500);

                var clientStatus = await server.GetClientStatusAsync();
                Assert.AreEqual(0, clientStatus.Count);

                var client2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("b"));
                await client2.PublishAsync("x", "1");
                await client2.PublishAsync("x", "2");
                await client2.PublishAsync("x", "3");
                await client2.DisconnectAsync();

                await Task.Delay(500);

                clientStatus = await server.GetClientStatusAsync();
                var sessionStatus = await server.GetSessionStatusAsync();

                Assert.AreEqual(0, clientStatus.Count);
                Assert.AreEqual(2, sessionStatus.Count);

                Assert.AreEqual(3, sessionStatus.First(s => s.ClientId == client1.Options.ClientId).PendingApplicationMessagesCount);
            }
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
                var c1MessageHandler = testEnvironment.CreateApplicationMessageHandler(c1);
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());

                var c2 = await testEnvironment.ConnectClient(new MqttClientOptionsBuilder().WithClientId("sender"));
                await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
                await Task.Delay(500);
                
                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(500);
                await c1.UnsubscribeAsync(topicFilter);
                await Task.Delay(500);

                Assert.AreEqual(expectedReceivedMessagesCount, c1MessageHandler.ReceivedApplicationMessages.Count);
            }
        }

        [TestMethod]
        public async Task Intercept_Undelivered()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var undeliverd = string.Empty;

                var options = new MqttServerOptionsBuilder().WithUndeliveredMessageInterceptor(
                    context => { undeliverd = context.ApplicationMessage.Topic; });

                await testEnvironment.StartServer(options);

                var client = await testEnvironment.ConnectClient();

                await client.SubscribeAsync("b");

                await client.PublishAsync("a", null, MqttQualityOfServiceLevel.ExactlyOnce);

                await Task.Delay(500);

                Assert.AreEqual("a", undeliverd);
            }
        }

        [TestMethod]
        public async Task Intercept_ClientMessageQueue()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder()
                    .WithClientMessageQueueInterceptor(c => c.ApplicationMessage.Topic = "a"));

                var topicAReceived = false;
                var topicBReceived = false;

                var client = await testEnvironment.ConnectClient();
                client.UseApplicationMessageReceivedHandler(c =>
                {
                    if (c.ApplicationMessage.Topic == "a")
                    {
                        topicAReceived = true;
                    }
                    else if (c.ApplicationMessage.Topic == "b")
                    {
                        topicBReceived = true;
                    }
                });

                await client.SubscribeAsync("a");
                await client.SubscribeAsync("b");

                await client.PublishAsync("b");

                await Task.Delay(500);

                Assert.IsTrue(topicAReceived);
                Assert.IsFalse(topicBReceived);
            }
        }

        [TestMethod]
        public async Task Intercept_ClientMessageQueue_Different_QoS_Of_Subscription_And_Message()
        {
            const string topic = "a";

            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer(new MqttServerOptionsBuilder()
                    .WithClientMessageQueueInterceptor(c => { })); // Interceptor does nothing but has to be present.

                bool receivedMessage = false;
                var client = await testEnvironment.ConnectClient();
                client.UseApplicationMessageReceivedHandler(c => { receivedMessage = true; });

                await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);

                await client.PublishAsync(new MqttApplicationMessage { Topic = topic, QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

                await Task.Delay(500);

                Assert.IsTrue(receivedMessage);
            }
        }
    }
}