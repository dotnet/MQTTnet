using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class Server_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Use_Empty_Client_ID()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

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
                1,
                TestContext);
        }

        [TestMethod]
        public async Task Publish_At_Least_Once_0x01()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                1,
                TestContext);
        }

        [TestMethod]
        public async Task Publish_Exactly_Once_0x02()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                1,
                TestContext);
        }

        [TestMethod]
        public async Task Use_Clean_Session()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var client = testEnvironment.CreateClient();
                var connectResult = await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost", testEnvironment.ServerPort).WithCleanSession().Build());

                Assert.IsFalse(connectResult.IsSessionPresent);
            }
        }

        [TestMethod]
        public async Task Will_Message_Do_Not_Send()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage);

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClientAsync(clientOptions);
                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(1000);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage);

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClientAsync(clientOptions);
                c2.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Intercept_Subscription()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder().WithSubscriptionInterceptor(
                    c =>
                    {
                        // Set the topic to "a" regards what the client wants to subscribe.
                        c.TopicFilter.Topic = "a";
                    }));

                var topicAReceived = false;
                var topicBReceived = false;

                var client = await testEnvironment.ConnectClientAsync();
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

                await client.SubscribeAsync("b");

                await client.PublishAsync("a");

                await Task.Delay(500);

                Assert.IsTrue(topicAReceived);
                Assert.IsFalse(topicBReceived);
            }
        }

        [TestMethod]
        public async Task Subscribe_Unsubscribe()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                var server = await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("c1"));
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                var c2 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("c2"));

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
                await c2.PublishAsync(message);

                await Task.Delay(500);
                Assert.AreEqual(0, receivedMessagesCount);

                var subscribeEventCalled = false;
                server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(e =>
                {
                    subscribeEventCalled = e.TopicFilter.Topic == "a" && e.ClientId == c1.Options.ClientId;
                });

                await c1.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
                await Task.Delay(250);
                Assert.IsTrue(subscribeEventCalled, "Subscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(250);
                Assert.AreEqual(1, receivedMessagesCount);

                var unsubscribeEventCalled = false;
                server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(e =>
                {
                    unsubscribeEventCalled = e.TopicFilter == "a" && e.ClientId == c1.Options.ClientId;
                });

                await c1.UnsubscribeAsync("a");
                await Task.Delay(250);
                Assert.IsTrue(unsubscribeEventCalled, "Unsubscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(500);
                Assert.AreEqual(1, receivedMessagesCount);

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Multiple_In_Single_Request()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("a")
                    .WithTopicFilter("b")
                    .WithTopicFilter("c")
                    .Build());

                var c2 = await testEnvironment.ConnectClientAsync();

                await c2.PublishAsync("a");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 1);

                await c2.PublishAsync("b");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 2);

                await c2.PublishAsync("c");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 3);
            }
        }

        [TestMethod]
        public async Task Subscribe_Lots_In_Single_Request()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                var optionsBuilder = new MqttClientSubscribeOptionsBuilder();
                for (var i = 0; i < 500; i++)
                {
                    optionsBuilder.WithTopicFilter(i.ToString(), MqttQualityOfServiceLevel.AtMostOnce);
                }

                await c1.SubscribeAsync(optionsBuilder.Build()).ConfigureAwait(false);

                var c2 = await testEnvironment.ConnectClientAsync();

                var messageBuilder = new MqttApplicationMessageBuilder();
                for (var i = 0; i < 500; i++)
                {
                    messageBuilder.WithTopic(i.ToString());

                    await c2.PublishAsync(messageBuilder.Build()).ConfigureAwait(false);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 500, 5000);

                Assert.AreEqual(500, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Lots_In_Multiple_Requests()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                for (var i = 0; i < 500; i++)
                {
                    var so = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(i.ToString()).Build();

                    await c1.SubscribeAsync(so).ConfigureAwait(false);

                    await Task.Delay(10);
                }

                var c2 = await testEnvironment.ConnectClientAsync();

                var messageBuilder = new MqttApplicationMessageBuilder();
                for (var i = 0; i < 500; i++)
                {
                    messageBuilder.WithTopic(i.ToString());

                    await c2.PublishAsync(messageBuilder.Build()).ConfigureAwait(false);

                    await Task.Delay(10);
                }

                SpinWait.SpinUntil(() => receivedMessagesCount == 500, 5000);

                Assert.AreEqual(500, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Subscribe_Multiple_In_Multiple_Request()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("a")
                    .Build());

                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("b")
                    .Build());

                await c1.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("c")
                    .Build());

                var c2 = await testEnvironment.ConnectClientAsync();

                await c2.PublishAsync("a");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 1);

                await c2.PublishAsync("b");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 2);

                await c2.PublishAsync("c");
                await Task.Delay(100);
                Assert.AreEqual(receivedMessagesCount, 3);
            }
        }

        [TestMethod]
        public async Task Publish_From_Server()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();

                var receivedMessagesCount = 0;

                var client = await testEnvironment.ConnectClientAsync();
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
            var locked = new object();

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                var c2 = await testEnvironment.ConnectClientAsync();

                c1.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (locked)
                    {
                        receivedMessagesCount++;
                    }
                });

                c2.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (locked)
                    {
                        receivedMessagesCount++;
                    }
                });

                await c1.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }).ConfigureAwait(false);

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();

                for (var i = 0; i < 500; i++)
                {
                    await c1.PublishAsync(message).ConfigureAwait(false);
                }

                await Task.Delay(5000);

                Assert.AreEqual(1000, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Session_Takeover()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var options = new MqttClientOptionsBuilder()
                    .WithCleanSession(false)
                    .WithClientId("a");

                var client1 = await testEnvironment.ConnectClientAsync(options);
                await Task.Delay(500);

                var client2 = await testEnvironment.ConnectClientAsync(options);
                await Task.Delay(500);

                Assert.IsFalse(client1.IsConnected);
                Assert.IsTrue(client2.IsConnected);
            }
        }

        [TestMethod]
        public async Task No_Messages_If_No_Subscription()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var client = await testEnvironment.ConnectClientAsync();
                var receivedMessages = new List<MqttApplicationMessage>();

                client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                {
                    await client.PublishAsync("Connected");
                });

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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();
                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e => server.SubscribeAsync(e.ClientId, "topic1"));

                var client = await testEnvironment.ConnectClientAsync();
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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());

                var disconnectCalled = 0;

                var c1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder());
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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());

                var clientConnectedCalled = 0;
                var clientDisconnectedCalled = 0;

                server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(_ => Interlocked.Increment(ref clientConnectedCalled));
                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(_ => Interlocked.Increment(ref clientDisconnectedCalled));

                var c1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder());

                Assert.AreEqual(1, clientConnectedCalled);
                Assert.AreEqual(0, clientDisconnectedCalled);

                await Task.Delay(500);

                await c1.DisconnectAsync();

                await Task.Delay(500);

                Assert.AreEqual(1, clientConnectedCalled);
                Assert.AreEqual(1, clientDisconnectedCalled);
            }
        }

        [TestMethod]
        public async Task Client_Disconnect_Without_Errors()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                bool clientWasConnected;

                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());
                try
                {
                    var client = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder());

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

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();

                var tasks = new List<Task>();
                for (var i = 0; i < ClientCount; i++)
                {
                    var i2 = i;
                    var testEnvironment2 = testEnvironment;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using (var client = await testEnvironment2.ConnectClientAsync())
                            {
                                // Clear retained message.
                                await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                    .WithPayload(new byte[0]).WithRetainFlag().Build());

                                // Set retained message.
                                await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i2)
                                    .WithPayload("value").WithRetainFlag().Build());

                                await client.DisconnectAsync();
                            }
                        }
                        catch (Exception exception)
                        {
                            testEnvironment2.TrackException(exception);
                        }
                    }));
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
        public async Task Retained_Messages_Flow()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var retainedMessage = new MqttApplicationMessageBuilder().WithTopic("r").WithPayload("r").WithRetainFlag().Build();

                await testEnvironment.StartServerAsync();
                var c1 = await testEnvironment.ConnectClientAsync();

                var receivedMessages = 0;

                var c2 = await testEnvironment.ConnectClientAsync();
                c2.UseApplicationMessageReceivedHandler(c =>
                {
                    Interlocked.Increment(ref receivedMessages);
                });

                await c1.PublishAsync(retainedMessage);
                await c1.DisconnectAsync();
                await Task.Delay(500);

                for (var i = 0; i < 5; i++)
                {
                    await c2.UnsubscribeAsync("r");
                    await Task.Delay(100);
                    Assert.AreEqual(i, receivedMessages);

                    await c2.SubscribeAsync("r");
                    await Task.Delay(100);
                    Assert.AreEqual(i + 1, receivedMessages);
                }

                await c2.DisconnectAsync();
            }
        }

        [TestMethod]
        public async Task Receive_No_Retained_Message_After_Subscribe()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var receivedMessagesCount = 0;

                var c2 = await testEnvironment.ConnectClientAsync();
                c2.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained_other").Build());

                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Receive_Retained_Message_After_Subscribe()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var receivedMessages = new List<MqttApplicationMessage>();

                var c2 = await testEnvironment.ConnectClientAsync();
                c2.UseApplicationMessageReceivedHandler(c =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(c.ApplicationMessage);
                    }
                });

                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(500);

                Assert.AreEqual(1, receivedMessages.Count);
                Assert.IsTrue(receivedMessages.First().Retain);
            }
        }

        [TestMethod]
        public async Task Clear_Retained_Message_With_Empty_Payload()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[0]).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClientAsync();

                c2.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Clear_Retained_Message_With_Null_Payload()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var c1 = await testEnvironment.ConnectClientAsync();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload((byte[])null).WithRetainFlag().Build());

                await c1.DisconnectAsync();

                var c2 = await testEnvironment.ConnectClientAsync();

                c2.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));

                await Task.Delay(200);
                await c2.SubscribeAsync(new MqttTopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Intercept_Application_Message()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(
                    new MqttServerOptionsBuilder().WithApplicationMessageInterceptor(
                        c => { c.ApplicationMessage = new MqttApplicationMessage { Topic = "new_topic" }; }));

                string receivedTopic = null;
                var c1 = await testEnvironment.ConnectClientAsync();
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

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder().WithStorage(serverStorage));

                var c1 = await testEnvironment.ConnectClientAsync();

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());

                await Task.Delay(500);

                Assert.AreEqual(1, serverStorage.Messages.Count);
            }
        }

        [TestMethod]
        public async Task Publish_After_Client_Connects()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync();
                server.UseClientConnectedHandler(async e =>
                {
                    await server.PublishAsync("/test/1", "true", MqttQualityOfServiceLevel.ExactlyOnce, false);
                });

                string receivedTopic = null;

                var c1 = await testEnvironment.ConnectClientAsync();
                c1.UseApplicationMessageReceivedHandler(e => { receivedTopic = e.ApplicationMessage.Topic; });
                await c1.SubscribeAsync("#");

                await testEnvironment.ConnectClientAsync();
                await testEnvironment.ConnectClientAsync();
                await testEnvironment.ConnectClientAsync();
                await testEnvironment.ConnectClientAsync();

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

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder().WithApplicationMessageInterceptor(Interceptor));

                var c1 = await testEnvironment.ConnectClientAsync();
                var c2 = await testEnvironment.ConnectClientAsync();
                await c2.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("test").Build());

                var isIntercepted = false;
                c2.UseApplicationMessageReceivedHandler(c =>
                {
                    isIntercepted = string.Compare("extended", Encoding.UTF8.GetString(c.ApplicationMessage.Payload), StringComparison.Ordinal) == 0;
                });

                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("test").Build());
                await c1.DisconnectAsync();

                await Task.Delay(500);

                Assert.IsTrue(isIntercepted);
            }
        }

        [TestMethod]
        public async Task Send_Long_Body()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
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

                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());
                var client1 = await testEnvironment.ConnectClientAsync();
                client1.UseApplicationMessageReceivedHandler(c =>
                {
                    receivedBody = c.ApplicationMessage.Payload;
                });

                await client1.SubscribeAsync("string");

                var client2 = await testEnvironment.ConnectClientAsync();
                await client2.PublishAsync("string", longBody);

                await Task.Delay(500);

                Assert.IsTrue(longBody.SequenceEqual(receivedBody ?? new byte[0]));
            }
        }

        [TestMethod]
        public async Task Deny_Connection()
        {
            var serverOptions = new MqttServerOptionsBuilder().WithConnectionValidator(context =>
            {
                context.ReasonCode = MqttConnectReasonCode.NotAuthorized;
            });

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;

                await testEnvironment.StartServerAsync(serverOptions);

                var connectingFailedException = await Assert.ThrowsExceptionAsync<MqttConnectingFailedException>(() => testEnvironment.ConnectClientAsync());
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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;

                _connected = new Dictionary<string, bool>();
                var options = new MqttServerOptionsBuilder();
                options.WithConnectionValidator(e => ConnectionValidationHandler(e));
                var server = await testEnvironment.StartServerAsync(options);

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
                var c1 = await testEnvironment.ConnectClientAsync(clientOptions);

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
                    await testEnvironment.ConnectClientAsync(clientOptions);
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
            using (var testEnvironment = new TestEnvironment())
            {
                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());

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
                var c1 = await testEnvironment.ConnectClientAsync(clientOptionsBuilder);

                await Task.Delay(500);

                var flow = string.Join(string.Empty, events);
                Assert.AreEqual("c", flow);

                // dc
                // Connect client with same client ID. Should disconnect existing client.
                var c2 = await testEnvironment.ConnectClientAsync(clientOptionsBuilder);

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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());

                var clientOptions = new MqttClientOptionsBuilder();
                var c1 = await testEnvironment.ConnectClientAsync(clientOptions);
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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                testEnvironment.IgnoreClientLogErrors = true;

                var server = await testEnvironment.StartServerAsync();

                await testEnvironment.ConnectClientAsync();
                await server.StopAsync();

                try
                {
                    await testEnvironment.ConnectClientAsync();
                    Assert.Fail("Connecting should fail.");
                }
                catch (Exception)
                {
                }

                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultEndpointPort(testEnvironment.ServerPort).Build());
                await testEnvironment.ConnectClientAsync();
            }
        }

        [TestMethod]
        public async Task Do_Not_Send_Retained_Messages_For_Denied_Subscription()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder().WithSubscriptionInterceptor(c =>
                {
                    // This should lead to no subscriptions for "n" at all. So also no sending of retained messages.
                    if (c.TopicFilter.Topic == "n")
                    {
                        c.AcceptSubscription = false;
                    }
                }));

                // Prepare some retained messages.
                var client1 = await testEnvironment.ConnectClientAsync();
                await client1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("y").WithPayload("x").WithRetainFlag().Build());
                await client1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("n").WithPayload("x").WithRetainFlag().Build());
                await client1.DisconnectAsync();

                await Task.Delay(500);

                // Subscribe to all retained message types.
                // It is important to do this in a range of filters to ensure that a subscription is not "hidden".
                var client2 = await testEnvironment.ConnectClientAsync();

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
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var server = await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder().WithPersistentSessions());

                // Create the session including the subscription.
                var client1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("a"));
                await client1.SubscribeAsync("x");
                await client1.DisconnectAsync();
                await Task.Delay(500);

                var clientStatus = await server.GetClientStatusAsync();
                Assert.AreEqual(0, clientStatus.Count);

                var client2 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("b"));
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

        private static async Task TestPublishAsync(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            string topicFilter,
            MqttQualityOfServiceLevel filterQualityOfServiceLevel,
            int expectedReceivedMessagesCount,
            TestContext testContext)
        {
            using (var testEnvironment = new TestEnvironment(testContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder());

                var c1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("receiver"));
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());

                var c2 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithClientId("sender"));
                await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());
                await c2.DisconnectAsync().ConfigureAwait(false);

                await Task.Delay(500);
                await c1.UnsubscribeAsync(topicFilter);
                await Task.Delay(500);

                Assert.AreEqual(expectedReceivedMessagesCount, receivedMessagesCount);
            }
        }

        [TestMethod]
        public async Task Intercept_Undelivered()
        {
            using (var testEnvironment = new TestEnvironment())
            {
                var undeliverd = string.Empty;

                var options = new MqttServerOptionsBuilder().WithUndeliveredMessageInterceptor(
                    context =>
                    {
                        undeliverd = context.ApplicationMessage.Topic;
                    });

                await testEnvironment.StartServerAsync(options);

                var client = await testEnvironment.ConnectClientAsync();

                await client.SubscribeAsync("b");

                await client.PublishAsync("a", null, MqttQualityOfServiceLevel.ExactlyOnce);

                await Task.Delay(500);

                Assert.AreEqual("a", undeliverd);
            }
        }

        [TestMethod]
        public async Task Intercept_ClientMessageQueue()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder()
                    .WithClientMessageQueueInterceptor(c => c.ApplicationMessage.Topic = "a"));

                var topicAReceived = false;
                var topicBReceived = false;

                var client = await testEnvironment.ConnectClientAsync();
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

            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                await testEnvironment.StartServerAsync(new MqttServerOptionsBuilder()
                    .WithClientMessageQueueInterceptor(c => { })); // Interceptor does nothing but has to be present.

                bool receivedMessage = false;
                var client = await testEnvironment.ConnectClientAsync();
                client.UseApplicationMessageReceivedHandler(c =>
                {
                    receivedMessage = true;
                });

                await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);

                await client.PublishAsync(new MqttApplicationMessage { Topic = topic, QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

                await Task.Delay(500);

                Assert.IsTrue(receivedMessage);
            }
        }
    }
}
