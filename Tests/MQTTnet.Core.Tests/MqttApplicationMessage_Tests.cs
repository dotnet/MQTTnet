// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Extensions;
using MQTTnet.Packets;
using System.Collections.Generic;

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
            //Assert.AreEqual(1011, message.GetUserProperty<int>("value"));
            Assert.AreEqual(null, message.GetUserProperty("case"));
            Assert.AreEqual(null, message.GetUserProperty("nonExists"));
            //Assert.AreEqual(null, message.GetUserProperty<int?>("nonExists"));
            //Assert.ThrowsException<InvalidOperationException>(() => message.GetUserProperty<int>("nonExists"));
        }
    }
}
