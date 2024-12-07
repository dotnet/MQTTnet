using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using System;
using System.Buffers;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttConnectionMiddlewareTest
    {
        [TestMethod]
        public void IsMqttRequestTest()
        {
            var mqttv31Request = Convert.FromHexString("102800044d51545404c0003c0008636c69656e7469640008757365726e616d650008706173736f777264");
            var mqttv50Request = Convert.FromHexString("102900044d51545405c0003c000008636c69656e7469640008757365726e616d650008706173736f777264");

            var isMqttv31 = MqttConnectionMiddleware.IsMqttRequest(new ReadOnlySequence<byte>(mqttv31Request));
            var isMqttv50 = MqttConnectionMiddleware.IsMqttRequest(new ReadOnlySequence<byte>(mqttv50Request));

            Assert.IsTrue(isMqttv31);
            Assert.IsTrue(isMqttv50);
        }
    }
}
