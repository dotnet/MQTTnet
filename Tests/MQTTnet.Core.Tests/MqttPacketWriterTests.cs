﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketWriterTests
    {
        [TestMethod]
        public void WritePacket()
        {
            var writer = new MqttPacketWriter();
            Assert.AreEqual(0, writer.Length);

            writer.WriteWithLengthPrefix("1234567890");
            Assert.AreEqual(10 + 2, writer.Length);

            writer.WriteWithLengthPrefix(new byte[300]);
            Assert.AreEqual(300 + 2 + 12, writer.Length);

            writer.WriteWithLengthPrefix(new byte[5000]);
            Assert.AreEqual(5000 + 2 + 300 + 2 + 12, writer.Length);
        }
    }
}
