// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MQTTnet.Packets;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class MqttTopicFilterEquality_Tests
    {
        [TestMethod]
        public void TopicsEqual()
        {
            string simpleTopic = "/alpha/beta/gamma";
            MqttTopicFilterBuilder builder = new MqttTopicFilterBuilder();

            MqttTopicFilter filterA = builder
                .WithTopic( simpleTopic )
                .Build();
            MqttTopicFilter filterB = builder
                .WithTopic( simpleTopic )
                .Build();

            Assert.AreEqual( filterA, filterB );
        }

        [TestMethod]
        public void TopicsNotEqual()
        {
            string simpleTopicA = "/alpha/beta/gamma";
            string simpleTopicB = "/alpha/beta/omega";
            MqttTopicFilterBuilder builder = new MqttTopicFilterBuilder();

            MqttTopicFilter filterA = builder
                .WithTopic( simpleTopicA )
                .Build();
            MqttTopicFilter filterB = builder
                .WithTopic( simpleTopicB )
                .Build();

            Assert.AreNotEqual( filterA, filterB );
        }
        [TestMethod]
        public void TopicFiltersIdentity()
        {
            string simpleTopic = "/alpha/beta/gamma";
            MqttTopicFilterBuilder builder = new MqttTopicFilterBuilder();

            MqttTopicFilter filterA = builder
                .WithTopic( simpleTopic )
                .Build();
            MqttTopicFilter filterB = filterA;
            Assert.AreEqual( filterA, filterB );
        }
        [TestMethod]
        public void TopicFilterNull()
        {
            string simpleTopic = "/alpha/beta/gamma";
            MqttTopicFilterBuilder builder = new MqttTopicFilterBuilder();

            MqttTopicFilter filterA = builder
                .WithTopic( simpleTopic )
                .Build();
            Assert.AreNotEqual( filterA, null );
            Assert.AreNotEqual( filterA, simpleTopic ); // string os not a topicFilter
            Assert.AreNotEqual( filterA, 42 );          // value type
        }
    }
}

