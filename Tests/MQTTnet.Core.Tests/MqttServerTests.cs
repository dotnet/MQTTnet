using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttServerTests
    {
        [TestMethod]
        public void MqttServer_PublishSimple_AtMostOnce()
        {
            TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                1).Wait();
        }

        [TestMethod]
        public void MqttServer_PublishSimple_AtLeastOnce()
        {
            TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                1).Wait();
        }

        [TestMethod]
        public void MqttServer_PublishSimple_ExactlyOnce()
        {
            TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                1).Wait();
        }

        [TestMethod]
        public async Task MqttServer_Will_Message()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();
                var c1 = await serverAdapter.ConnectTestClient("c1");
                var c2 = await serverAdapter.ConnectTestClient("c2", willMessage);

                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
                await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());

                await c2.DisconnectAsync();

                await Task.Delay(1000);

                await c1.DisconnectAsync();
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Subscribe_Unsubscribe()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;

            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                var c2 = await serverAdapter.ConnectTestClient("c2");
                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();

                await c2.PublishAsync(message);
                await Task.Delay(1000);
                Assert.AreEqual(0, receivedMessagesCount);

                var subscribeEventCalled = false;
                s.ClientSubscribedTopic += (_, e) =>
                    {
                        subscribeEventCalled = e.TopicFilter.Topic == "a" && e.ClientId == "c1";
                    };

                await c1.SubscribeAsync(new TopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
                await Task.Delay(500);
                Assert.IsTrue(subscribeEventCalled, "Subscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(500);
                Assert.AreEqual(1, receivedMessagesCount);

                var unsubscribeEventCalled = false;
                s.ClientUnsubscribedTopic += (_, e) =>
                {
                    unsubscribeEventCalled = e.TopicFilter == "a" && e.ClientId == "c1";
                };

                await c1.UnsubscribeAsync("a");
                await Task.Delay(500);
                Assert.IsTrue(unsubscribeEventCalled, "Unsubscribe event not called.");

                await c2.PublishAsync(message);
                await Task.Delay(1000);
                Assert.AreEqual(1, receivedMessagesCount);
            }
            finally
            {
                await s.StopAsync();
            }
            await Task.Delay(500);

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Publish()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());
            var receivedMessagesCount = 0;

            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");

                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
                await c1.SubscribeAsync(new TopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

                await s.PublishAsync(message);
                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Publish_Multiple_Clients()
        {
            var s = new MqttFactory().CreateMqttServer();
            var receivedMessagesCount = 0;
            var locked = new object();

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .Build();

            var clientOptions2 = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .Build();

            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = new MqttFactory().CreateMqttClient();
                var c2 = new MqttFactory().CreateMqttClient();

                await c1.ConnectAsync(clientOptions);
                await c2.ConnectAsync(clientOptions2);

                c1.ApplicationMessageReceived += (_, __) =>
                {
                    lock (locked)
                    {
                        receivedMessagesCount++;
                    }
                };

                c2.ApplicationMessageReceived += (_, __) =>
                {
                    lock (locked)
                    {
                        receivedMessagesCount++;
                    }
                };

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
                await c1.SubscribeAsync(new TopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
                await c2.SubscribeAsync(new TopicFilter { Topic = "a", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

                //await Task.WhenAll(Publish(c1, message), Publish(c2, message));
                await Publish(c1, message);

                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(2000, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Session_Takeover()
        {
            var server = new MqttFactory().CreateMqttServer();
            try
            {
                await server.StartAsync(new MqttServerOptions());

                var client1 = new MqttFactory().CreateMqttClient();
                var client2 = new MqttFactory().CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .WithCleanSession(false)
                    .WithClientId("a").Build();

                await client1.ConnectAsync(options);

                await Task.Delay(500);

                await client2.ConnectAsync(options);

                await Task.Delay(500);

                Assert.IsFalse(client1.IsConnected);
                Assert.IsTrue(client2.IsConnected);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task MqttServer_No_Messages_If_No_Subscription()
        {
            var server = new MqttFactory().CreateMqttServer();
            try
            {
                await server.StartAsync(new MqttServerOptions());

                var client = new MqttFactory().CreateMqttClient();
                var receivedMessages = new List<MqttApplicationMessage>();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost").Build();

                client.Connected += async (s, e) =>
                {
                    await client.PublishAsync("Connected");
                };

                client.ApplicationMessageReceived += (s, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }
                };

                await client.ConnectAsync(options);

                await Task.Delay(500);

                await client.PublishAsync("Hello");

                await Task.Delay(500);
                
                Assert.AreEqual(0, receivedMessages.Count);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task MqttServer_Set_Subscription_At_Server()
        {
            var server = new MqttFactory().CreateMqttServer();
            try
            {
                await server.StartAsync(new MqttServerOptions());
                server.ClientConnected += async (s, e) =>
                {
                    await server.SubscribeAsync(e.ClientId, "topic1");
                };

                var client = new MqttFactory().CreateMqttClient();
                var receivedMessages = new List<MqttApplicationMessage>();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost").Build();

                client.ApplicationMessageReceived += (s, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }
                };

                await client.ConnectAsync(options);

                await Task.Delay(500);

                await client.PublishAsync("Hello");

                await Task.Delay(500);

                Assert.AreEqual(0, receivedMessages.Count);
            }
            finally
            {
                await server.StopAsync();
            }
        }

        private static async Task Publish(IMqttClient c1, MqttApplicationMessage message)
        {
            for (int i = 0; i < 1000; i++)
            {
                await c1.PublishAsync(message);
            }
        }

        [TestMethod]
        public async Task MqttServer_Shutdown_Disconnects_Clients_Gracefully()
        {
            using (var testSetup = new TestSetup())
            {
                var server = await testSetup.StartServerAsync(new MqttServerOptionsBuilder());

                var disconnectCalled = 0;

                var c1 = await testSetup.ConnectClientAsync(new MqttClientOptionsBuilder());
                c1.Disconnected += (sender, args) => disconnectCalled++;

                await Task.Delay(100);

                await server.StopAsync();

                await Task.Delay(100);

                Assert.AreEqual(1, disconnectCalled);
            }
        }

        [TestMethod]
        public async Task MqttServer_Handle_Clean_Disconnect()
        {
            using (var testSetup = new TestSetup())
            {
                var server = await testSetup.StartServerAsync(new MqttServerOptionsBuilder());

                var clientConnectedCalled = 0;
                var clientDisconnectedCalled = 0;

                server.ClientConnected += (_, __) => Interlocked.Increment(ref clientConnectedCalled);
                server.ClientDisconnected += (_, __) => Interlocked.Increment(ref clientDisconnectedCalled);
                
                var c1 = await testSetup.ConnectClientAsync(new MqttClientOptionsBuilder());

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
        public async Task MqttServer_Client_Disconnect_Without_Errors()
        {
            using (var testSetup = new TestSetup())
            {
                bool clientWasConnected;

                var server = await testSetup.StartServerAsync(new MqttServerOptionsBuilder());
                try
                {
                    var client = await testSetup.ConnectClientAsync(new MqttClientOptionsBuilder());

                    clientWasConnected = true;

                    await client.DisconnectAsync();

                    await Task.Delay(500);
                }
                finally
                {
                    await server.StopAsync();
                }

                Assert.IsTrue(clientWasConnected);

                testSetup.ThrowIfLogErrors();
            }
        }

        [TestMethod]
        public async Task MqttServer_Lots_Of_Retained_Messages()
        {
            const int ClientCount = 100;

            var server = new MqttFactory().CreateMqttServer();
            try
            {
                await server.StartAsync(new MqttServerOptionsBuilder().Build());

                Parallel.For(
                    0,
                    ClientCount,
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    i =>
                {
                    using (var client = new MqttFactory().CreateMqttClient())
                    {
                        client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build())
                            .GetAwaiter().GetResult();

                        for (var j = 0; j < 10; j++)
                        {
                            // Clear retained message.
                            client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i)
                                .WithPayload(new byte[0]).WithRetainFlag().Build()).GetAwaiter().GetResult();

                            // Set retained message.
                            client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("r" + i)
                                .WithPayload("value" + j).WithRetainFlag().Build()).GetAwaiter().GetResult();
                        }

                        Thread.Sleep(100);

                        client.DisconnectAsync().GetAwaiter().GetResult();
                    }
                });

                await Task.Delay(1000);

                var retainedMessages = server.GetRetainedMessages();

                Assert.AreEqual(ClientCount, retainedMessages.Count);

                for (var i = 0; i < ClientCount; i++)
                {
                    Assert.IsTrue(retainedMessages.Any(m => m.Topic == "r" + i));
                }
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task MqttServer_Retained_Messages_Flow()
        {
            var retainedMessage = new MqttApplicationMessageBuilder().WithTopic("r").WithPayload("r").WithRetainFlag().Build();
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());
            await s.StartAsync(new MqttServerOptions());
            var c1 = await serverAdapter.ConnectTestClient("c1");
            await c1.PublishAsync(retainedMessage);
            await Task.Delay(500);
            await c1.DisconnectAsync();
            await Task.Delay(500);

            var receivedMessages = 0;
            var c2 = await serverAdapter.ConnectTestClient("c2");
            c2.ApplicationMessageReceived += (_, e) =>
            {
                receivedMessages++;
            };

            for (var i = 0; i < 5; i++)
            {
                await c2.UnsubscribeAsync("r");
                await Task.Delay(500);
                Assert.AreEqual(i, receivedMessages);

                await c2.SubscribeAsync("r");
                await Task.Delay(500);
                Assert.AreEqual(i + 1, receivedMessages);
            }

            await c2.DisconnectAsync();

            await s.StopAsync();
        }

        [TestMethod]
        public async Task MqttServer_No_Retained_Message()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;

            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                await c1.PublishAsync(builder => builder.WithTopic("retained").WithPayload(new byte[3]));
                await c1.DisconnectAsync();

                var c2 = await serverAdapter.ConnectTestClient("c2");
                c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
                await c2.SubscribeAsync(new TopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Retained_Message()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessages = new List<MqttApplicationMessage>();
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                await c1.PublishAndWaitForAsync(s, new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await serverAdapter.ConnectTestClient("c2");
                c2.ApplicationMessageReceived += (_, e) =>
                {
                    lock (receivedMessages)
                    {
                        receivedMessages.Add(e.ApplicationMessage);
                    }
                };

                await c2.SubscribeAsync(new TopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(1, receivedMessages.Count);
            Assert.IsTrue(receivedMessages.First().Retain);
        }

        [TestMethod]
        public async Task MqttServer_Clear_Retained_Message()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                await c1.PublishAsync(builder => builder.WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag());
                await c1.PublishAsync(builder => builder.WithTopic("retained").WithPayload(new byte[0]).WithRetainFlag());
                await c1.DisconnectAsync();

                var c2 = await serverAdapter.ConnectTestClient("c2");
                c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                await Task.Delay(200);
                await c2.SubscribeAsync(new TopicFilter { Topic = "retained", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }


            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Persist_Retained_Message()
        {
            var storage = new TestStorage();
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            try
            {
                var options = new MqttServerOptions { Storage = storage };

                await s.StartAsync(options);

                var c1 = await serverAdapter.ConnectTestClient("c1");

                await c1.PublishAndWaitForAsync(s, new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());

                await Task.Delay(250);

                await c1.DisconnectAsync();
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(1, storage.Messages.Count);

            s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;
            try
            {
                var options = new MqttServerOptions { Storage = storage };
                await s.StartAsync(options);

                var c2 = await serverAdapter.ConnectTestClient("c2");
                c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
                await c2.SubscribeAsync(new TopicFilterBuilder().WithTopic("retained").Build());

                await Task.Delay(250);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Intercept_Message()
        {
            void Interceptor(MqttApplicationMessageInterceptorContext context)
            {
                context.ApplicationMessage.Payload = Encoding.ASCII.GetBytes("extended");
            }

            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            try
            {
                var options = new MqttServerOptions { ApplicationMessageInterceptor = new MqttServerApplicationMessageInterceptorDelegate(c => Interceptor(c)) };

                await s.StartAsync(options);

                var c1 = await serverAdapter.ConnectTestClient("c1");
                var c2 = await serverAdapter.ConnectTestClient("c2");
                await c2.SubscribeAsync(new TopicFilterBuilder().WithTopic("test").Build());

                var isIntercepted = false;
                c2.ApplicationMessageReceived += (sender, args) =>
                {
                    isIntercepted = string.Compare("extended", Encoding.UTF8.GetString(args.ApplicationMessage.Payload), StringComparison.Ordinal) == 0;
                };

                await c1.PublishAsync(builder => builder.WithTopic("test"));
                await c1.DisconnectAsync();

                await Task.Delay(500);

                Assert.IsTrue(isIntercepted);
            }
            finally
            {
                await s.StopAsync();
            }
        }

        [TestMethod]
        public async Task MqttServer_Body()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var bodyIsMatching = false;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                var c2 = await serverAdapter.ConnectTestClient("c2");

                c1.ApplicationMessageReceived += (_, e) =>
                {
                    if (Encoding.UTF8.GetString(e.ApplicationMessage.Payload) == "The body")
                    {
                        bodyIsMatching = true;
                    }
                };

                await c1.SubscribeAsync("A", MqttQualityOfServiceLevel.AtMostOnce);
                await c2.PublishAsync(builder => builder.WithTopic("A").WithPayload(Encoding.UTF8.GetBytes("The body")));

                await Task.Delay(1000);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.IsTrue(bodyIsMatching);
        }

        [TestMethod]
        public async Task MqttServer_Connection_Denied()
        {
            var server = new MqttFactory().CreateMqttServer();
            var client = new MqttFactory().CreateMqttClient();

            try
            {
                var options = new MqttServerOptionsBuilder().WithConnectionValidator(context =>
                {
                    context.ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized;
                }).Build();

                await server.StartAsync(options);


                var clientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost").Build();

                try
                {
                    await client.ConnectAsync(clientOptions);
                    Assert.Fail("An exception should be raised.");
                }
                catch (Exception exception)
                {
                    if (exception is MqttConnectingFailedException)
                    {

                    }
                    else
                    {
                        Assert.Fail("Wrong exception.");
                    }
                }
            }
            finally
            {
                await client.DisconnectAsync();
                await server.StopAsync();

                client.Dispose();
            }
        }

        [TestMethod]
        public async Task MqttServer_Same_Client_Id_Connect_Disconnect_Event_Order()
        {
            using (var testSetup = new TestSetup())
            {
                var server = await testSetup.StartServerAsync(new MqttServerOptionsBuilder());

                var events = new List<string>();

                server.ClientConnected += (_, __) =>
                {
                    lock (events)
                    {
                        events.Add("c");
                    }
                };

                server.ClientDisconnected += (_, __) =>
                {
                    lock (events)
                    {
                        events.Add("d");
                    }
                };

                var clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId("same_id");

                // c
                var c1 = await testSetup.ConnectClientAsync(clientOptions);
                
                await Task.Delay(500);

                var flow = string.Join(string.Empty, events);
                Assert.AreEqual("c", flow);

                // dc
                var c2 = await testSetup.ConnectClientAsync(clientOptions);

                await Task.Delay(500);

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdc", flow);

                // nothing
                await c1.DisconnectAsync();

                await Task.Delay(500);

                // d
                await c2.DisconnectAsync();

                await Task.Delay(500);

                await server.StopAsync();

                flow = string.Join(string.Empty, events);
                Assert.AreEqual("cdcd", flow);
            }
        }

        [TestMethod]
        public async Task MqttServer_Remove_Session()
        {
            using (var testSetup = new TestSetup())
            {
                var server = await testSetup.StartServerAsync(new MqttServerOptionsBuilder());

                var clientOptions = new MqttClientOptionsBuilder();
                var c1 = await testSetup.ConnectClientAsync(clientOptions);
                await Task.Delay(500);
                Assert.AreEqual(1, (await server.GetClientSessionsStatusAsync()).Count);

                await c1.DisconnectAsync();
                await Task.Delay(500);

                Assert.AreEqual(0, (await server.GetClientSessionsStatusAsync()).Count);
            }
        }

        [TestMethod]
        public async Task MqttServer_Stop_And_Restart()
        {
            var server = new MqttFactory().CreateMqttServer();
            await server.StartAsync(new MqttServerOptions());

            var client = new MqttFactory().CreateMqttClient();
            await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());
            await server.StopAsync();

            try
            {
                var client2 = new MqttFactory().CreateMqttClient();
                await client2.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());

                Assert.Fail("Connecting should fail.");
            }
            catch (Exception)
            {
            }

            await server.StartAsync(new MqttServerOptions());
            var client3 = new MqttFactory().CreateMqttClient();
            await client3.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());

            await server.StopAsync();
        }

        [TestMethod]
        public async Task MqttServer_Close_Idle_Connection()
        {
            var server = new MqttFactory().CreateMqttServer();

            try
            {
                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(4)).Build());

                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync("localhost", 1883);

                // Don't send anything. The server should close the connection.
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ReceiveAsync(new ArraySegment<byte>(new byte[10]), SocketFlags.Partial);
                    Assert.Fail("Receive should throw an exception.");
                }
                catch (SocketException)
                {
                }
            }
            finally
            {
                await server.StopAsync();
            }
        }

        [TestMethod]
        public async Task MqttServer_Send_Garbage()
        {
            var server = new MqttFactory().CreateMqttServer();

            try
            {
                await server.StartAsync(new MqttServerOptionsBuilder().WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(4)).Build());

                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync("localhost", 1883);
                await client.SendAsync(Encoding.UTF8.GetBytes("Garbage"), SocketFlags.None);

                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ReceiveAsync(new ArraySegment<byte>(new byte[10]), SocketFlags.Partial);
                    Assert.Fail("Receive should throw an exception.");
                }
                catch (SocketException)
                {
                }
            }
            finally
            {
                await server.StopAsync();
            }
        }

        private class TestStorage : IMqttServerStorage
        {
            public IList<MqttApplicationMessage> Messages = new List<MqttApplicationMessage>();

            public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                Messages = messages;
                return Task.CompletedTask;
            }

            public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
            {
                return Task.FromResult(Messages);
            }
        }

        private static async Task TestPublishAsync(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            string topicFilter,
            MqttQualityOfServiceLevel filterQualityOfServiceLevel,
            int expectedReceivedMessagesCount)
        {
            var s = new MqttFactory().CreateMqttServer();

            var receivedMessagesCount = 0;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = new MqttFactory().CreateMqttClient();
                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
                await c1.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());
                await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());

                var c2 = new MqttFactory().CreateMqttClient();
                await c2.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost").Build());
                await c2.PublishAsync(builder => builder.WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel));

                await Task.Delay(500);
                await c1.UnsubscribeAsync(topicFilter);
                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.AreEqual(expectedReceivedMessagesCount, receivedMessagesCount);
        }
    }
}
