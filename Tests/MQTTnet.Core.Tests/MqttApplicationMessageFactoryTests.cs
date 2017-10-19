using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttApplicationMessageFactoryTests
    {
        [TestMethod]
        public void CreateApplicationMessage_TopicOnly()
        {
            var message = new MqttApplicationMessageFactory().CreateApplicationMessage("Abc", MqttQualityOfServiceLevel.AtLeastOnce);
            Assert.AreEqual("Abc", message.Topic);
            Assert.IsFalse(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);
        }

        [TestMethod]
        public void CreateApplicationMessage_TimeStampPayload()
        {
            var message = new MqttApplicationMessageFactory().CreateApplicationMessage("xyz", TimeSpan.FromSeconds(360));
            Assert.AreEqual("xyz", message.Topic);
            Assert.IsFalse(message.Retain);
            Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
            Assert.AreEqual(Encoding.UTF8.GetString(message.Payload), "00:06:00");
        }
    }
}
