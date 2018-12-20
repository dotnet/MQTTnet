using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttSubscriptionsManagerTests
    {
        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServerEventDispatcher());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServerEventDispatcher());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

            sm.Subscribe(sp);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServerEventDispatcher());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter { Topic = "#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
            sp.TopicFilters.Add(new TopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

            sm.Subscribe(sp);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServerEventDispatcher());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            Assert.IsFalse(sm.CheckSubscriptions("A/B/X", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServerEventDispatcher());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            Assert.IsTrue(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            sm.Unsubscribe(up);

            Assert.IsFalse(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce).IsSubscribed);
        }
    }
}
