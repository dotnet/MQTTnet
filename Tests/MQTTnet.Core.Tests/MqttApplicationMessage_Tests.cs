using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Extensions;
using MQTTnet.Packets;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttApplicationMessage_Tests
    {
        [TestMethod]
        public void GetUserProperty_Test()
        {
            var message = new MqttApplicationMessage
            {
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("foo", "bar"),
                    new MqttUserProperty("value", "1011"),
                    new MqttUserProperty("CASE", "insensitive")
                }
            };

            Assert.AreEqual("bar", message.GetUserProperty("foo"));
            Assert.AreEqual(1011, message.GetUserProperty<int>("value"));
            Assert.AreEqual("insensitive", message.GetUserProperty("case"));
            Assert.AreEqual(null, message.GetUserProperty("nonExists"));
            Assert.AreEqual(null, message.GetUserProperty<int?>("nonExists"));
            Assert.ThrowsException<InvalidOperationException>(() => message.GetUserProperty<int>("nonExists"));
        }
    }
}
