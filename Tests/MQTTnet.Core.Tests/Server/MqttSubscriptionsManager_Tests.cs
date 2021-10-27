using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class MqttSubscriptionsManager_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerOptions(), new MqttServerEventContainer(new TestLogger()), new MqttRetainedMessagesManager());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.Subscribe(sp, CancellationToken.None);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerOptions(), new MqttServerEventContainer(new TestLogger()), new MqttRetainedMessagesManager());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

            await sm.Subscribe(sp, CancellationToken.None);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerOptions(), new MqttServerEventContainer(new TestLogger()), new MqttRetainedMessagesManager());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });
            sp.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });

            await sm.Subscribe(sp, CancellationToken.None);

            var result = sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerOptions(), new MqttServerEventContainer(new TestLogger()), new MqttRetainedMessagesManager());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.Subscribe(sp, CancellationToken.None);

            Assert.IsFalse(sm.CheckSubscriptions("A/B/X", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var s = CreateSession();

            var sm = new MqttClientSubscriptionsManager(s, new MqttServerOptions(), new MqttServerEventContainer(new TestLogger()), new MqttRetainedMessagesManager());

            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await sm.Subscribe(sp, CancellationToken.None);

            Assert.IsTrue(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            await sm.Unsubscribe(up, CancellationToken.None);

            Assert.IsFalse(sm.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }

        MqttClientSession CreateSession()
        {
            var logger = new TestLogger();
            var options = new MqttServerOptions();
            var retainedMessagesManager = new MqttRetainedMessagesManager();
            var eventContainer = new MqttServerEventContainer(logger);
                
            return new MqttClientSession(
                "",
                new ConcurrentDictionary<object, object>(),
                options,
                eventContainer,
                retainedMessagesManager,
                new MqttClientSessionsManager(options, retainedMessagesManager, eventContainer, logger));
        }
    }
}
