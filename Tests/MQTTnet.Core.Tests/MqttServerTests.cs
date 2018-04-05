using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Client;

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
                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                var c2 = await serverAdapter.ConnectTestClient(s, "c2", willMessage);

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

            Assert.AreEqual(1, receivedMessagesCount);
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();

                await c2.PublishAsync(message);
                Assert.AreEqual(0, receivedMessagesCount);

                var subscribeEventCalled = false;
                s.ClientSubscribedTopic += (_, e) =>
                    {
                        subscribeEventCalled = e.TopicFilter.Topic == "a" && e.ClientId == "c1";
                    };

                await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));
                await Task.Delay(100);
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
                await Task.Delay(100);
                Assert.IsTrue(unsubscribeEventCalled, "Unsubscribe event not called.");

                await c2.PublishAsync(message);

                await Task.Delay(500);
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");

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
        public async Task MqttServer_NoRetainedMessage()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttFactory().CreateMqttServer(new[] { serverAdapter }, new MqttNetLogger());

            var receivedMessagesCount = 0;

            try
            {
                await s.StartAsync(new MqttServerOptions());

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).Build());
                await c1.DisconnectAsync();

                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                await c1.PublishAndWaitForAsync(s, new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.DisconnectAsync();

                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
                await c1.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[0]).WithRetainFlag().Build());
                await c1.DisconnectAsync();
                
                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
                c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");

                await c1.PublishAndWaitForAsync(s, new MqttApplicationMessageBuilder().WithTopic("retained").WithPayload(new byte[3]).WithRetainFlag().Build());
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

                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                var c2 = await serverAdapter.ConnectTestClient(s, "c2");
                await c2.SubscribeAsync(new TopicFilterBuilder().WithTopic("test").Build());

                var isIntercepted = false;
                c2.ApplicationMessageReceived += (sender, args) =>
                {
                    isIntercepted = string.Compare("extended", Encoding.UTF8.GetString(args.ApplicationMessage.Payload), StringComparison.Ordinal) == 0;
                };

                var m = new MqttApplicationMessageBuilder().WithTopic("test").Build();
                await c1.PublishAsync(m);
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                var c2 = await serverAdapter.ConnectTestClient(s, "c2");

                c1.ApplicationMessageReceived += (_, e) =>
                {
                    if (Encoding.UTF8.GetString(e.ApplicationMessage.Payload) == "The body")
                    {
                        bodyIsMatching = true;
                    }
                };

                await c1.SubscribeAsync("A", MqttQualityOfServiceLevel.AtMostOnce);
                await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("A").WithPayload(Encoding.UTF8.GetBytes("The body")).Build());

                await Task.Delay(500);
            }
            finally
            {
                await s.StopAsync();
            }

            Assert.IsTrue(bodyIsMatching);
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

                var c1 = await serverAdapter.ConnectTestClient(s, "c1");
                var c2 = await serverAdapter.ConnectTestClient(s, "c2");

                c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

                await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());
                await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());

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
