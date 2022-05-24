// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeAndUnsubscribeSingle()
        {
            var sp = new MqttSubscribePacket
            {
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilterBuilder().WithTopic("A/B/C").Build()
                }
            };

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            Assert.IsTrue(CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);

            var up = new MqttUnsubscribePacket();
            up.TopicFilters.Add("A/B/C");
            await _subscriptionsManager.Unsubscribe(up, CancellationToken.None);

            Assert.IsFalse(CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeDifferentQoSSuccess()
        {
            var sp = new MqttSubscribePacket
            {
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce }
                }
            };

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");
            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleNoSuccess()
        {
            var sp = new MqttSubscribePacket
            {
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilterBuilder().WithTopic("A/B/C").Build()
                }
            };

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            Assert.IsFalse(CheckSubscriptions("A/B/X", MqttQualityOfServiceLevel.AtMostOnce, "").IsSubscribed);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeSingleSuccess()
        {
            var sp = new MqttSubscribePacket
            {
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilterBuilder().WithTopic("A/B/C").Build()
                }
            };

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.AtMostOnce, "");

            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtMostOnce);
        }

        [TestMethod]
        public async Task MqttSubscriptionsManager_SubscribeTwoTimesSuccess()
        {
            var sp = new MqttSubscribePacket
            {
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilter { Topic = "#", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce },
                    new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce }
                }
            };

            await _subscriptionsManager.Subscribe(sp, CancellationToken.None);

            var result = CheckSubscriptions("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce, "");

            Assert.IsTrue(result.IsSubscribed);
            Assert.AreEqual(result.QualityOfServiceLevel, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            var logger = new TestLogger();
            var options = new MqttServerOptions();
            var retainedMessagesManager = new MqttRetainedMessagesManager(new MqttServerEventContainer(), logger);
            var eventContainer = new MqttServerEventContainer();
            var clientSessionManager = new MqttClientSessionsManager(options, retainedMessagesManager, eventContainer, logger);

            var session = new MqttSession("", false, new ConcurrentDictionary<object, object>(), options, eventContainer, retainedMessagesManager, clientSessionManager);

            _subscriptionsManager = new MqttClientSubscriptionsManager(session, new MqttServerEventContainer(), retainedMessagesManager, clientSessionManager);
        }

        CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel applicationMessageQoSLevel, string senderClientId)
        {
            ulong topicHashMask; // not needed
            bool hasWildcard; // not needed
            MqttSubscription.CalculateTopicHash(topic, out var topicHash, out topicHashMask, out hasWildcard);
            return _subscriptionsManager.CheckSubscriptions(topic, topicHash, applicationMessageQoSLevel, senderClientId);
        }
    }
}