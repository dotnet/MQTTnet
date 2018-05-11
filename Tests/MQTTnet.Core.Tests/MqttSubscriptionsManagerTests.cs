using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
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
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServer(new IMqttServerAdapter[0], new MqttNetLogger().CreateChildLogger("")));

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            var result = sm.CheckSubscriptionsAsync(pp).Result;
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServer(new IMqttServerAdapter[0], new MqttNetLogger().CreateChildLogger("")));

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtMostOnce));

            sm.Subscribe(sp);

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            };

            var result = sm.CheckSubscriptionsAsync(pp).Result;
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServer(new IMqttServerAdapter[0], new MqttNetLogger().CreateChildLogger("")));

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilter("#", MqttQualityOfServiceLevel.AtMostOnce));
            sp.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.AtLeastOnce));

            sm.Subscribe(sp);

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
            };

            var result = sm.CheckSubscriptionsAsync(pp).Result;
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServer(new IMqttServerAdapter[0], new MqttNetLogger().CreateChildLogger("")));

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/X",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsFalse(sm.CheckSubscriptionsAsync(pp).Result.IsSubscribed);
        }

        [TestMethod]
        public void MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sm = new MqttClientSubscriptionsManager("", new MqttServerOptions(), new MqttServer(new IMqttServerAdapter[0], new MqttNetLogger().CreateChildLogger("")));

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new TopicFilterBuilder().WithTopic("A/B/C").Build());

            sm.Subscribe(sp);

            var pp = new MqttApplicationMessage
            {
                Topic = "A/B/C",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            Assert.IsTrue(sm.CheckSubscriptionsAsync(pp).Result.IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            sm.Unsubscribe(up);

            Assert.IsFalse(sm.CheckSubscriptionsAsync(pp).Result.IsSubscribed);
        }
    }
}
