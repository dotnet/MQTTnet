using System;
using System.Collections.Concurrent;
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
        public async Task MqttServer_PublishSimple_AtMostOnce()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtMostOnce,
                1);
        }

        [TestMethod]
        public async Task MqttServer_PublishSimple_AtLeastOnce()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.AtLeastOnce,
                1);
        }

        [TestMethod]
        public async Task MqttServer_PublishSimple_ExactlyOnce()
        {
            await TestPublishAsync(
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                "A/B/C",
                MqttQualityOfServiceLevel.ExactlyOnce,
                1);
        }

        [TestMethod]
        public async Task MqttServer_WillMessage()
        {
            var s = new MqttServer(new MqttServerOptions(), new TestMqttServerAdapter());
            s.Start();

            var willMessage = new MqttApplicationMessage("My/last/will", new byte[0], MqttQualityOfServiceLevel.AtMostOnce, false);
            var c1 = ConnectTestClient("c1", null,  s);
            var c2 = ConnectTestClient("c2", willMessage, s);

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;
            await c1.SubscribeAsync(new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce));

            await c2.DisconnectAsync();

            await Task.Delay(1000);

            s.Stop();

            Assert.AreEqual(1, receivedMessagesCount);
        }

        private MqttClient ConnectTestClient(string clientId, MqttApplicationMessage willMessage, MqttServer server)
        {
            var adapterA = new TestMqttClientAdapter();
            var adapterB = new TestMqttClientAdapter();
            adapterA.Partner = adapterB;
            adapterB.Partner = adapterA;

            var client = new MqttClient(new MqttClientOptions(), adapterA);
            server.InjectClient(clientId, adapterB);
            client.ConnectAsync(willMessage).Wait();
            return client;
        }

        private async Task TestPublishAsync(
            string topic,
            MqttQualityOfServiceLevel qualityOfServiceLevel,
            string topicFilter,
            MqttQualityOfServiceLevel filterQualityOfServiceLevel,
            int expectedReceivedMessagesCount)
        {
            var s = new MqttServer(new MqttServerOptions(), new TestMqttServerAdapter());
            s.Start();

            var c1 = ConnectTestClient("c1", null, s);
            var c2 = ConnectTestClient("c2", null, s);

            var receivedMessagesCount = 0;
            c1.ApplicationMessageReceived += (_, __) => receivedMessagesCount++;

            await c1.SubscribeAsync(new TopicFilter(topicFilter, filterQualityOfServiceLevel));
            await c2.PublishAsync(new MqttApplicationMessage(topic, new byte[0], qualityOfServiceLevel, false));

            await Task.Delay(500);
            await c1.Unsubscribe(topicFilter);

            await Task.Delay(500);

            s.Stop();

            Assert.AreEqual(expectedReceivedMessagesCount, receivedMessagesCount);
        }
    }

    public class TestMqttClientAdapter : IMqttCommunicationAdapter
    {
        private readonly BlockingCollection<MqttBasePacket> _incomingPackets = new BlockingCollection<MqttBasePacket>();

        public TestMqttClientAdapter Partner { get; set; }

        public async Task ConnectAsync(MqttClientOptions options, TimeSpan timeout)
        {
            await Task.FromResult(0);
        }

        public async Task DisconnectAsync()
        {
            await Task.FromResult(0);
        }

        public async Task SendPacketAsync(MqttBasePacket packet, TimeSpan timeout)
        {
            ThrowIfPartnerIsNull();

            Partner.SendPacketInternal(packet);
            await Task.FromResult(0);
        }

        public async Task<MqttBasePacket> ReceivePacketAsync(TimeSpan timeout)
        {
            ThrowIfPartnerIsNull();

            return await Task.Run(() => _incomingPackets.Take());
        }

        private void SendPacketInternal(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            _incomingPackets.Add(packet);
        }

        private void ThrowIfPartnerIsNull()
        {
            if (Partner == null)
            {
                throw new InvalidOperationException("Partner is not set.");
            }
        }
    }
}
