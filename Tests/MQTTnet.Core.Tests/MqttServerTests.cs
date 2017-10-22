using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

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
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();
            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            var c2 = await serverAdapter.ConnectTestClient(s, "c2", willMessage);

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c1.SubscribeAsync(new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce));

            await c2.DisconnectAsync();

            await Task.Delay(1000);

            await s.StopAsync();

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Unsubscribe()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            var c2 = await serverAdapter.ConnectTestClient(s, "c2");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();

            await c2.PublishAsync(message);
            Assert.AreEqual(0, receivedMessagesCount);

            await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));
            await c2.PublishAsync(message);

            await Task.Delay(500);
            Assert.AreEqual(1, receivedMessagesCount);

            await c1.UnsubscribeAsync("a");
            await c2.PublishAsync(message);

            await Task.Delay(500);
            Assert.AreEqual(1, receivedMessagesCount);

            await s.StopAsync();
            await Task.Delay(500);

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_Publish()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            var message = new MqttApplicationMessageBuilder().WithTopic("a").WithAtLeastOnceQoS().Build();
            await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));

            s.Publish(message);
            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_NoRetainedMessage()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            await c1.PublishAsync(new MqttApplicationMessage("retained", new byte[3], MqttQualityOfServiceLevel.AtLeastOnce, false));
            await c1.DisconnectAsync();

            var c2 = await serverAdapter.ConnectTestClient(s, "c2");
            var receivedMessagesCount = 0;
            c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c2.SubscribeAsync(new TopicFilter("retained", MqttQualityOfServiceLevel.AtLeastOnce));

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_RetainedMessage()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            await c1.PublishAsync(new MqttApplicationMessage("retained", new byte[3], MqttQualityOfServiceLevel.AtLeastOnce, true));
            await c1.DisconnectAsync();

            var c2 = await serverAdapter.ConnectTestClient(s, "c2");
            var receivedMessagesCount = 0;
            c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c2.SubscribeAsync(new TopicFilter("retained", MqttQualityOfServiceLevel.AtLeastOnce));

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_ClearRetainedMessage()
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            await c1.PublishAsync(new MqttApplicationMessage("retained", new byte[3], MqttQualityOfServiceLevel.AtLeastOnce, true));
            await c1.PublishAsync(new MqttApplicationMessage("retained", new byte[0], MqttQualityOfServiceLevel.AtLeastOnce, true));
            await c1.DisconnectAsync();

            var c2 = await serverAdapter.ConnectTestClient(s, "c2");
            var receivedMessagesCount = 0;
            c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c2.SubscribeAsync(new TopicFilter("retained", MqttQualityOfServiceLevel.AtLeastOnce));

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(0, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_PersistRetainedMessage()
        {
            var storage = new TestStorage();

            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions { Storage = storage }, new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            await c1.PublishAsync(new MqttApplicationMessage("retained", new byte[3], MqttQualityOfServiceLevel.AtLeastOnce, true));
            await c1.DisconnectAsync();

            await s.StopAsync();

            s = new MqttServer(new MqttServerOptions { Storage = storage }, new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c2 = await serverAdapter.ConnectTestClient(s, "c2");
            var receivedMessagesCount = 0;
            c2.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c2.SubscribeAsync(new TopicFilter("retained", MqttQualityOfServiceLevel.AtLeastOnce));

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(1, receivedMessagesCount);
        }

        [TestMethod]
        public async Task MqttServer_InterceptMessage()
        {
            var options = new MqttServerOptions
            {
                ApplicationMessageInterceptor = message =>
                {
                    message.Payload = Encoding.ASCII.GetBytes("extended");
                    return message;
                }
            };

            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(options, new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

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

        private class TestStorage : IMqttServerStorage
        {
            private IList<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();

            public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                _messages = messages;
                return Task.CompletedTask;
            }

            public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
            {
                return Task.FromResult(_messages);
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
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter }, new MqttNetTrace());
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            var c2 = await serverAdapter.ConnectTestClient(s, "c2");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicFilter).WithQualityOfServiceLevel(filterQualityOfServiceLevel).Build());
            await c2.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(new byte[0]).WithQualityOfServiceLevel(qualityOfServiceLevel).Build());

            await Task.Delay(500);
            await c1.UnsubscribeAsync(topicFilter);

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(expectedReceivedMessagesCount, receivedMessagesCount);
        }
    }
}
