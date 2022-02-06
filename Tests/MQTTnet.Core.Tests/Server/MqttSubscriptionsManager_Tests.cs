// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class MqttSubscriptionsManager_Tests : BaseTestClass
    {
        MqttClientSubscriptionsManager _subscriptionsManager;

        [TestInitialize]
        public void TestInitialize()
        {
            var logger = new TestLogger();
            var options = new MqttServerOptions();
            var retainedMessagesManager = new MqttRetainedMessagesManager(new MqttServerEventContainer(), logger);
            var eventContainer = new MqttServerEventContainer();

            var session = new MqttSession(
                "",
                false,
                new ConcurrentDictionary<object, object>(),
                options,
                eventContainer,
                retainedMessagesManager,
                new MqttClientSessionsManager(options, retainedMessagesManager, eventContainer, logger));

            _subscriptionsManager = new MqttClientSubscriptionsManager(session, new MqttServerEventContainer(),
                new MqttRetainedMessagesManager(new MqttServerEventContainer(), new MqttNetNullLogger()));
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = _subscriptionsManager.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter {Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce});

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = _subscriptionsManager.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilter {Topic = "#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce});
            sp.TopicFilters.Add(new MqttTopicFilter {Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce});

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = _subscriptionsManager.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            Assert.IsFalse(_subscriptionsManager.CheckSubscriptions("A/B/X", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sp = new MqttSubscribePacket();
            sp.TopicFilters.Add(new MqttTopicFilterBuilder().WithTopic("A/B/C").Build());

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            Assert.IsTrue(_subscriptionsManager.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            await _subscriptionsManager.Unsubscribe(up, CancellationToken.None);

            Assert.IsFalse(_subscriptionsManager.CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }
    }
}