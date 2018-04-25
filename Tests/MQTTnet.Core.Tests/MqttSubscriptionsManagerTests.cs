using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttSubscriptionsManagerTests
    {
        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var sm = new MqttClientSubscriptionsManager(new MqttServerOptions(), "");

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.SubscribeAsync(sp).Wait();

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            var result = sm.CheckSubscriptions(pp);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var sm = new MqttClientSubscriptionsManager(new MqttServerOptions(), "");

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtMostOnce));

            sm.SubscribeAsync(sp).Wait();

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            };

            var result = sm.CheckSubscriptions(pp);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var sm = new MqttClientSubscriptionsManager(new MqttServerOptions(), "");

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce));
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtLeastOnce));

            sm.SubscribeAsync(sp).Wait();

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            };

            var result = sm.CheckSubscriptions(pp);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sm = new MqttClientSubscriptionsManager(new MqttServerOptions(), "");

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.SubscribeAsync(sp).Wait();

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/X",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsFalse(sm.CheckSubscriptions(pp).IsSubscribed);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sm = new MqttClientSubscriptionsManager(new MqttServerOptions(), "");

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.SubscribeAsync(sp).Wait();

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsTrue(sm.CheckSubscriptions(pp).IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            sm.UnsubscribeAsync(up).Wait();

            Assert.IsFalse(sm.CheckSubscriptions(pp).IsSubscribed);
        }
    }
}
