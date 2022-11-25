using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    [TestClass]
    public class PayloadSegmentExtensionsTest
    {
        [TestMethod]
        public void GetSetPayload()
        {
            var message = new Message();
            Assert.AreEqual(0, message.GetPayloadSegment().Count);

            message.Payload = new byte[] { 1, 2, 3 };
            Assert.AreEqual(3, message.GetPayloadSegment().Count);

            message.PayloadLength = 1;
            Assert.AreEqual(1, message.GetPayloadSegment().Count);
            Assert.IsTrue(message.GetPayloadSegment().SequenceEqual(new byte[] { 1 }));

            message.PayloadLength = null;
            message.PayloadOffset = 1;
            Assert.AreEqual(2, message.GetPayloadSegment().Count);
            Assert.IsTrue(message.GetPayloadSegment().SequenceEqual(new byte[] { 2, 3 }));


            var segmentInput = new ArraySegment<byte>(new byte[] { 4, 5, 6 }, 1, 2);
            message.SetPayloadSegment(segmentInput);
            var segmentOutput = message.GetPayloadSegment();
            Assert.IsTrue(segmentInput.SequenceEqual(segmentOutput));
        }

        private class Message : IPayloadSegmentable
        {
            public byte[] Payload { get; set; }
            public int PayloadOffset { get; set; }
            public int? PayloadLength { get; set; }
        }
    }
}
