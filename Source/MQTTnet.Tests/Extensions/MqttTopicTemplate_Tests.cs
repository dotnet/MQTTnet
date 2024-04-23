// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Assert.AreEqual("A/B/C/D", template.WithParameter("param", "D"));
            Assert.AreEqual("A/B/+/D", template.WithoutParameter("param"));
            Assert.AreEqual("A/B/+/D", template.TopicFilter);
            Assert.AreEqual("A/B/+/D/#", template.TopicTreeRootFilter);

            // corner cases
            Assert.AreEqual("", new MqttTopicTemplate("").TopicFilter);
            Assert.AreEqual("#", new MqttTopicTemplate("#").TopicFilter);
            Assert.AreEqual("+", new MqttTopicTemplate("+").TopicFilter);
            Assert.AreEqual("/", new MqttTopicTemplate("/").TopicFilter);
        }

        [TestMethod]
        public void BasicOpsWithHash()
        {
            var template = new MqttTopicTemplate("A/B/{param}/D/#");
            Assert.AreEqual("A/B/C/D/#", template.WithParameter("param", "D"));
            Assert.AreEqual("A/B/+/D/#", template.WithoutParameter("param"));
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
            template.WithParameter("foo", "e/f");
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
        public void CanonicalPrefixFilter()
        {
            var template1 = new MqttTopicTemplate("A/v1/{param}/F");
            var template2 = new MqttTopicTemplate("A/v1/E/F");
            var template3 = new MqttTopicTemplate("A/v1/E/F/G/H");
            var canonicalFilter = MqttTopicTemplate.FindCanonicalPrefix(new[] { template1, template2, template3 });
            Assert.AreEqual("A/v1/+/+", canonicalFilter.TopicFilter);
            Assert.AreEqual("A/v1/+/+/#", canonicalFilter.TopicTreeRootFilter);

            var template4 = new MqttTopicTemplate("A/v2/{prefix}");
            var canonicalFilter2 = MqttTopicTemplate.FindCanonicalPrefix(new[] { template1, template2, template3, template4 });
            Assert.AreEqual("A/+/+", canonicalFilter2.TopicFilter);
            Assert.AreEqual("A/+/#", canonicalFilter2.TopicTreeRootFilter);
        }


    }
}