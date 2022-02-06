// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Protocol;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttApplicationMessageBuilder_Tests
    {
        [TestMethod]
        public void CreateApplicationMessage_TopicOnly()
        {
            var message = new MqttApplicationMessageBuilder().WithTopic("Abc").Build();
            Assert.AreEqual("Abc", message.Topic);
            Assert.IsFalse(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
        }

        [TestMethod]
        public void CreateApplicationMessage_TimeStampPayload()
        {
            var message = new MqttApplicationMessageBuilder().WithTopic("xyz").WithPayload(TimeSpan.FromSeconds(360).ToString()).Build();
            Assert.AreEqual("xyz", message.Topic);
            Assert.IsFalse(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
            Assert.AreEqual(Encoding.UTF8.GetString(message.Payload), "00:06:00");
        }

        [TestMethod]
        public void CreateApplicationMessage_StreamPayload()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("xHello")) { Position = 1 };

            var message = new MqttApplicationMessageBuilder().WithTopic("123").WithPayload(stream).Build();
            Assert.AreEqual("123", message.Topic);
            Assert.IsFalse(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
            Assert.AreEqual(Encoding.UTF8.GetString(message.Payload), "Hello");
        }

        [TestMethod]
        public void CreateApplicationMessage_Retained()
        {
            var message = new MqttApplicationMessageBuilder().WithTopic("lol").WithRetainFlag().Build();
            Assert.AreEqual("lol", message.Topic);
            Assert.IsTrue(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
        }

        [TestMethod]
        public void CreateApplicationMessage_QosLevel2()
        {
            var message = new MqttApplicationMessageBuilder().WithTopic("rofl").WithRetainFlag().WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce).Build();
            Assert.AreEqual("rofl", message.Topic);
            Assert.IsTrue(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.ExactlyOnce, message.QualityOfServiceLevel);
        }
    }
}
