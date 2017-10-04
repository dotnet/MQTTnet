using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
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
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter });
            await s.StartAsync();

            var willMessage = new MqttApplicationMessage("My/last/will", new byte[0], MqttQualityOfServiceLevel.AtMostOnce, false);
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
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter });
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            var c2 = await serverAdapter.ConnectTestClient(s, "c2");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            
            var message = new MqttApplicationMessage("a", new byte[0], MqttQualityOfServiceLevel.AtLeastOnce, false);

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
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter });
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            var message = new MqttApplicationMessage("a", new byte[0], MqttQualityOfServiceLevel.AtLeastOnce, false);
            await c1.SubscribeAsync(new TopicFilter("a", MqttQualityOfServiceLevel.AtLeastOnce));

            s.Publish(message);
            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(1, receivedMessagesCount);
        }        

        private async Task TestPublishAsync(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            string topicFilter,
            MqttQualityOfServiceLevel filterQualityOfServiceLevel,
            int expectedReceivedMessagesCount)
        {
            var serverAdapter = new TestMqttServerAdapter();
            var s = new MqttServer(new MqttServerOptions(), new List<IMqttServerAdapter> { serverAdapter });
            await s.StartAsync();

            var c1 = await serverAdapter.ConnectTestClient(s, "c1");
            var c2 = await serverAdapter.ConnectTestClient(s, "c2");

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            await c1.SubscribeAsync(new TopicFilter(topicFilter, filterQualityOfServiceLevel));
            await c2.PublishAsync(new MqttApplicationMessage(topic, new byte[0], qualityOfServiceLevel, false));

            await Task.Delay(500);
            await c1.UnsubscribeAsync(topicFilter);

            await Task.Delay(500);

            await s.StopAsync();

            Assert.AreEqual(expectedReceivedMessagesCount, receivedMessagesCount);
        }
    }
}
