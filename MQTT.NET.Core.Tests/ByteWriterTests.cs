using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ByteWriterTests
    {
        [TestMethod]
        public void ByteWriter_WriteMultipleAll()
        {
            var b = new ByteWriter();
            Assert.AreEqual(0, b.Value);
            b.Write(3, 2);
            Assert.AreEqual(3, b.Value);
        }

        [TestMethod]
        public void ByteWriter_WriteMultiplePartial()
        {
            var b = new ByteWriter();
            Assert.AreEqual(0, b.Value);
            b.Write(255, 2);
            Assert.AreEqual(3, b.Value);
        }

        [TestMethod]
        public void ByteWriter_WriteTo0xFF()
        {
            var b = new ByteWriter();

            Assert.AreEqual(0, b.Value);
            b.Write(true);
            Assert.AreEqual(1, b.Value);
            b.Write(true);
            Assert.AreEqual(3, b.Value);
            b.Write(true);
            Assert.AreEqual(7, b.Value);
            b.Write(true);
            Assert.AreEqual(15, b.Value);
            b.Write(true);
            Assert.AreEqual(31, b.Value);
            b.Write(true);
            Assert.AreEqual(63, b.Value);
            b.Write(true);
            Assert.AreEqual(127, b.Value);
            b.Write(true);
            Assert.AreEqual(255, b.Value);
        }
    }
}
