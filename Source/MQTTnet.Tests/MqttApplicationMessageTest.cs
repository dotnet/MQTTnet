#pragma warning disable CS0618 // Type or member is obsolete

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class MqttApplicationMessageTest
    {
        [TestMethod]
        public void PayloadSegment()
        {
            var message = new MqttApplicationMessage();
            Assert.AreEqual(0, message.Payload.Length);
            Assert.AreEqual(0, message.PayloadSegment.Count);

            Assert.IsTrue(ReferenceEquals(message.Payload, message.PayloadSegment.Array));

            message.Payload = new byte[] { 1, 2 };
            Assert.AreEqual(2, message.Payload.Length);
            Assert.AreEqual(2, message.PayloadSegment.Count);
            Assert.IsTrue(ReferenceEquals(message.Payload, message.PayloadSegment.Array));

            message.Payload = new byte[] { 1, 2, 3 };
            Assert.AreEqual(3, message.Payload.Length);
            Assert.AreEqual(3, message.PayloadSegment.Count);
            Assert.IsTrue(ReferenceEquals(message.Payload, message.PayloadSegment.Array));

            message.PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3 });
            Assert.AreEqual(3, message.Payload.Length);
            Assert.AreEqual(3, message.PayloadSegment.Count);
            Assert.IsTrue(ReferenceEquals(message.Payload, message.PayloadSegment.Array));

            message.PayloadSegment = new ArraySegment<byte>(new byte[] { 1, 2, 3 }, 1, 1);
            Assert.AreEqual(1, message.Payload.Length);
            Assert.AreEqual(1, message.PayloadSegment.Count);
            Assert.IsFalse(ReferenceEquals(message.Payload, message.PayloadSegment.Array));
        }
    }
}
