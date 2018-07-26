using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Implementations;

namespace MQTTnet.Core.Tests
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
        public async Task MqttServer_WillMessage()
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
        public async Task MqttServer_SubscribeUnsubscribe()
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

                await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));
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
                await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));

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
        public async Task MqttServer_Publish_MultipleClients()
        {
            var serverAdapter = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());
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
                await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));
                await c2.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));

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

        private static async Task Publish(IMqttClient c1, MqttApplicationMessage message)
        {
            for (int i = 0; i < 1000; i++)
            {
                await c1.PublishAsync(message);
            }
        }
        
        [TestMethod]
        public async Task MqttServer_ShutdownDisconnectsClientsGracefully()
        {
            var serverAdapter = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .Build();

            var disconnectCalled = 0;

            await s.StartAsync(new MqttServerOptions());

            var c1 = new MqttFactory().CreateMqttClient();
            c1.Disconnected += (sender, args) => disconnectCalled++;

            await c1.ConnectAsync(clientOptions);

            await Task.Delay(100);

            await s.StopAsync();

            await Task.Delay(100);

            Assert.AreEqual(1, disconnectCalled);
        }

        [TestMethod]
        public async Task MqttServer_HandleCleanDisconnect()
        {
            MqttNetGlobalLogger.LogMessagePublished += (_, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[{e.TraceMessage.Timestamp:s}] {e.TraceMessage.Source} {e.TraceMessage.Message}");
            };

            var serverAdapter = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var clientConnectedCalled = 0;
            var clientDisconnectedCalled = 0;

            s.ClientConnected += (_, __) => clientConnectedCalled++;
            s.ClientDisconnected += (_, __) => clientDisconnectedCalled++;

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .Build();

            await s.StartAsync(new MqttServerOptions());

            var c1 = new MqttFactory().CreateMqttClient();

            await c1.ConnectAsync(clientOptions);

            await Task.Delay(100);

            await c1.DisconnectAsync();

            await Task.Delay(100);

            await s.StopAsync();

            await Task.Delay(100);

            Assert.AreEqual(clientConnectedCalled, clientDisconnectedCalled);
        }

        [TestMethod]
        public async Task MqttServer_RetainedMessagesFlow()
        {
            var retainedMessage = new MqttApplicationMessageBuilder().WithTopic("r").WithPayload("r").WithRetainFlag().Build();
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());
            await s.StartAsync(new MqttServerOptions());
            var c1 = await serverAdapter.ConnectTestClient("c1");
            await c1.PublishAsync(retainedMessage);
            Thread.Sleep(500);
            await c1.DisconnectAsync();
            Thread.Sleep(500);

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
        }

        [TestMethod]
        public async Task MqttServer_NoRetainedMessage()
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
        public async Task MqttServer_RetainedMessage()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                await c1.PublishAndWaitForAsync(s, new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
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

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_ClearRetainedMessage()
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
                await c2.SubscribeAsync(new TopicFilter("retained", MqttQualityOfServiceLevel.AtMostOnce));
                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }


            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_PersistRetainedMessage()
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
        public async Task MqttServer_InterceptMessage()
        {
            void Interceptor(MqttApplicationMessageInterceptorContext context)
            {
                context.ApplicationMessage.Payload = Encoding.ASCII.GetBytes("extended");
            }

            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            try
            {
                var options = new MqttServerOptions { ApplicationMessageInterceptor = Interceptor };

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
        public async Task MqttServer_SameClientIdConnectDisconnectEventOrder()
        {
            var serverAdapter = new MqttTcpServerAdapter(new MqttNetLogger().CreateChildLogger());
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var connectedClient = false;
            var connecteCalledBeforeConnectedClients = false;

            s.ClientConnected += (_, __) =>
            {
                connecteCalledBeforeConnectedClients |= connectedClient;
                connectedClient = true;
            };

            s.ClientDisconnected += (_, __) =>
            {
                connectedClient = false;
            };

            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost")
                .WithClientId(Guid.NewGuid().ToString())
                .Build();

            await s.StartAsync(new MqttServerOptions());

            var c1 = new MqttFactory().CreateMqttClient();
            var c2 = new MqttFactory().CreateMqttClient();

            await c1.ConnectAsync(clientOptions);

            await Task.Delay(100);

            await c2.ConnectAsync(clientOptions);

            await Task.Delay(100);

            await c1.DisconnectAsync();
            await c2.DisconnectAsync();

            await s.StopAsync();

            await Task.Delay(100);

            Assert.IsFalse(connecteCalledBeforeConnectedClients, "ClientConnected was called before ClientDisconnect was called");
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
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;
            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient("c1");
                var c2 = await serverAdapter.ConnectTestClient("c2");

                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());
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
