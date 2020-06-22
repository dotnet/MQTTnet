using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttSubscriptionsManager_Tests
    {
        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerEventDispatcher(new TestLogger()), new MqttServerOptions());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.SubscribeAsync(sp, new MqttConnectPacket());

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerEventDispatcher(new TestLogger()), new MqttServerOptions());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

            await sm.SubscribeAsync(sp, new MqttConnectPacket());

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerEventDispatcher(new TestLogger()), new MqttServerOptions());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

            await sm.SubscribeAsync(sp, new MqttConnectPacket());

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerEventDispatcher(new TestLogger()), new MqttServerOptions());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.SubscribeAsync(sp, new MqttConnectPacket());

            Assert.IsFalse(sm.CheckSubscriptions("A/B/X", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerEventDispatcher(new TestLogger()), new MqttServerOptions());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.SubscribeAsync(sp, new MqttConnectPacket());

            Assert.IsTrue(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            await sm.UnsubscribeAsync(up);

            Assert.IsFalse(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);
        }

        MqttClientSession CreateSession()
        {
            return new MqttClientSession(
                "",
                new ConcurrentDictionary<object, object>(),
                new MqttServerEventDispatcher(new TestLogger()),
                new MqttServerOptions(),
                new MqttRetainedMessagesManager(),
                new TestLogger());
        }
    }
}
