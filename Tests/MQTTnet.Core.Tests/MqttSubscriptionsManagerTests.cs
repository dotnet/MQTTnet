using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttSubscriptionsManagerTests
    {
        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var sm = new MqttClientSubscriptionsManager();

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtMostOnce));

            sm.Subscribe(sp);

            var pp = new MqttPublishPacket
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsTrue(sm.IsSubscribed(pp));
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sm = new MqttClientSubscriptionsManager();

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtMostOnce));

            sm.Subscribe(sp);

            var pp = new MqttPublishPacket
            {
                Topic = "A/B/X",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsFalse(sm.IsSubscribed(pp));
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sm = new MqttClientSubscriptionsManager();

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtMostOnce));

            sm.Subscribe(sp);

            var pp = new MqttPublishPacket
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsTrue(sm.IsSubscribed(pp));

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            sm.Unsubscribe(up);

            Assert.IsFalse(sm.IsSubscribed(pp));
        }
    }
}
