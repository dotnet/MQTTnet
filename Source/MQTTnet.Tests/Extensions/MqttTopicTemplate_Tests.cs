// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.TopicTemplate;

namespace MQTTnet.Tests.Extensions
{
    [TestClass]
    public sealed class MqttTopicTemplate_Tests
    {
        [TestMethod]
        public void BasicOps()
        {
            var template = new MqttTopicTemplate("A/B/{param}/D");
            Assert.AreEqual("A/B/C/D", template.WithParameter("param", "C").Template);
            Assert.AreEqual("A/B/+/D", template.WithoutParameter("param").Template);
            Assert.AreEqual("A/B/+/D", template.TopicFilter);
            Assert.AreEqual("A/B/+/D/#", template.TopicTreeRootFilter);

            // corner cases
            Assert.AreEqual("#", new MqttTopicTemplate("#").TopicFilter);
            Assert.AreEqual("+", new MqttTopicTemplate("+").TopicFilter);
            Assert.AreEqual("/", new MqttTopicTemplate("/").TopicFilter);
        }

        [TestMethod]
        public void BasicOpsWithHash()
        {
            var template = new MqttTopicTemplate("A/B/{param}/D/#");
            Assert.AreEqual("A/B/C/D/#", template.WithParameter("param", "C").Template);
            Assert.AreEqual("A/B/+/D/#", template.WithoutParameter("param").Template);
            Assert.AreEqual("A/B/+/D/#", template.TopicFilter);
            Assert.AreEqual("A/B/+/D/#", template.TopicTreeRootFilter);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RejectsReservedChars1()
        {
            var template = new MqttTopicTemplate("A/B/{foo}/D");
            template.WithParameter("foo", "a#");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RejectsReservedChars2()
        {
            var template = new MqttTopicTemplate("A/B/{foo}/D");
            template.WithParameter("foo", "a+b");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RejectsReservedChars3()
        {
            var template = new MqttTopicTemplate("A/B/{foo}/D");
            template.WithParameter("foo", "a/b");
        }
        
        [TestMethod]
        public void AcceptsEmptyValue()
        {
            var template = new MqttTopicTemplate("A/B/{foo}/D");
            template.WithParameter("foo", "");
        }
        
        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void RejectsEmptyTemplate()
        {
            var _ = new MqttTopicTemplate("");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RejectsNullTemplate()
        {
            var _ = new MqttTopicTemplate(null);
        }

        
        [TestMethod]
        public void IgnoresEmptyParameters()
        {
            var template = new MqttTopicTemplate("A/B/{}/D");
            Assert.IsFalse(template.Parameters.Any());
        }
        
        [TestMethod]
        public void AcceptsValidTopics()
        {
            _ = new MqttTopicTemplate("A");
            _ = new MqttTopicTemplate("/");
            _ = new MqttTopicTemplate("/A");
            _ = new MqttTopicTemplate("A/#");
            _ = new MqttTopicTemplate("$SYS/#");
            _ = new MqttTopicTemplate("////");
            _ = new MqttTopicTemplate("A/B/{foo}/D/");
        }

        [TestMethod]
        public void DynamicRoutingSupport()
        {
            var v1template = new MqttTopicTemplate("A/v1/{id}/F");
            var v2template = new MqttTopicTemplate("A/v2/foo/{id}/{bar}");
            var incoming_v1_topic = "A/v1/32/F";

            var compat_v2_topic = v2template
                .WithParameterValuesFrom(
                    v1template.ParseParameterValues(incoming_v1_topic))
                .WithParameter("bar", "unknown");

            // id: 32 was relocated and bar: unknown added
            Assert.AreEqual("A/v2/foo/32/unknown", compat_v2_topic.Template);
        }

        [TestMethod]
        public void SubscriptionSupport()
        {
            var template = new MqttTopicTemplate("A/v1/{param}/F");
            var filter = template.BuildFilter()
                .WithAtLeastOnceQoS()
                .WithNoLocal()
                .Build();
            Assert.AreEqual("A/v1/+/F", filter.Topic);
        }

        [TestMethod]
        public void SubscriptionSupport2()
        {
            var template = new MqttTopicTemplate("A/v1/{param}/F");
            
            var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                .WithTopicTemplate(template)
                .WithSubscriptionIdentifier(5)
                .Build();
            Assert.AreEqual("A/v1/+/F", subscribeOptions.TopicFilters[0].Topic);
        }

        [TestMethod]
        public void SendAndSubscribeSupport()
        {
            var template = new MqttTopicTemplate("App/v1/{sender}/message");

            var filter = new MqttTopicFilterBuilder()
                .WithTopicTemplate(template)
                .WithAtLeastOnceQoS()
                .WithNoLocal()
                .Build();

            Assert.AreEqual("App/v1/+/message", filter.Topic);

            var myTopic = template.WithParameter("sender", "me, myself & i");

            var message = new MqttApplicationMessageBuilder()
                .WithTopicTemplate(
                    myTopic)
                .WithPayload("Hello!")
                .Build();

            Assert.IsTrue(message.MatchesTopicTemplate(template));
        }

        [TestMethod]
        public void SendAndSubscribeSupport2()
        {
            var template = new MqttTopicTemplate("App/v1/{sender}/message");
            Assert.ThrowsException<ArgumentException>(() => 
                template.BuildMessage());
        }

        [TestMethod]
        public void CanonicalPrefixFilter()
        {
            var template1 = new MqttTopicTemplate("A/v1/{param}/F");
            var template2 = new MqttTopicTemplate("A/v1/E/F");
            var template3 = new MqttTopicTemplate("A/v1/E/F/G/H");
            var canonicalFilter = MqttTopicTemplate.FindCanonicalPrefix(new[] { template1, template2, template3 });
            // possible improvement: Assert.AreEqual("A/v1/+/F", canonicalFilter.TopicFilter);
            Assert.AreEqual("A/v1/+", canonicalFilter.TopicFilter);
            Assert.AreEqual("A/v1/+/#", canonicalFilter.TopicTreeRootFilter);
            
            var template2b = new MqttTopicTemplate("A/v1/E/X");
            canonicalFilter = MqttTopicTemplate.FindCanonicalPrefix(new[] { template1, template2, template3, template2b });
            Assert.AreEqual("A/v1/+", canonicalFilter.Template);
            Assert.AreEqual("A/v1/+", canonicalFilter.TopicFilter);
            Assert.AreEqual("A/v1/+/#", canonicalFilter.TopicTreeRootFilter);


            var template4 = new MqttTopicTemplate("A/v2/{prefix}");
            var canonicalFilter2 = MqttTopicTemplate.FindCanonicalPrefix(new[] { template1, template2, template3, template4 });
            Assert.AreEqual("A/+", canonicalFilter2.TopicFilter);
            Assert.AreEqual("A/+/#", canonicalFilter2.TopicTreeRootFilter);
        }
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple1()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A"),
                new MqttTopicTemplate("B")
            });
            Assert.AreEqual("#", template.TopicFilter);
            Assert.AreEqual("#", template.Template);
        }
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple2()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A/v1/{param}/F/"),
                new MqttTopicTemplate("A/v1/E/X")
            });
            Assert.AreEqual("A/v1/+", template.TopicFilter);
        }
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple3()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A/v1/{param}/F"),
                new MqttTopicTemplate("A/v1/{param}/F/X")
            });
            Assert.AreEqual("A/v1/+/+", template.TopicFilter);
            Assert.AreEqual("A/v1/{param}/+", template.Template);
        }
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple4()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A/v1/{param}/FFA"),
                new MqttTopicTemplate("A/v1/{param}/FX")
            });
            Assert.AreEqual("A/v1/+/+", template.TopicFilter);
            Assert.AreEqual("A/v1/{param}/+", template.Template);
        }
        
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple5()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A/v1/E/F/G/H"),
                new MqttTopicTemplate("A/v2/{param}")
            });
            Assert.AreEqual("A/+", template.TopicFilter);
            Assert.AreEqual("A/+/#", template.TopicTreeRootFilter);
        }
        
        [TestMethod]
        public void CanonicalPrefixFilterSimple6()
        {
            var template = MqttTopicTemplate.FindCanonicalPrefix(new[]
            {
                new MqttTopicTemplate("A/{version}/E/F/G/H"),
                new MqttTopicTemplate("A/{version}/{param}")
            });
            Assert.AreEqual("A/+/+", template.TopicFilter);
            Assert.AreEqual("A/+/+/#", template.TopicTreeRootFilter);
        }
    }
}