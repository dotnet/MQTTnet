using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ByteReaderTests
    {
        [TestMethod]
        public void ByteReader_ReadToEnd()
        {
            var reader = new ByteReader(85);
            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());
            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());
            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());
            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());
        }

        [TestMethod]
        public void ByteReader_ReadPartial()
        {
            var reader = new ByteReader(15);
            Assert.AreEqual(3, reader.Read(2));
        }
    }
}
