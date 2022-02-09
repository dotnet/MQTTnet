using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttPacketWriter_Tests
    {
        [TestMethod]
        public void Use_All_Data_Types()
        {
            var writer = new MqttPacketWriter();
            writer.WriteWithLengthPrefix("AString");
            writer.Write(0x1);
            writer.Write(0x0);
            writer.Write(0x1);
            writer.WriteVariableLengthInteger(1234U);
            writer.WriteVariableLengthInteger(9876U);

            var buffer = writer.GetBuffer();

            var reader = new MqttPacketBodyReader(buffer, 0, writer.Length);

            Assert.AreEqual("AString", reader.ReadStringWithLengthPrefix());
            Assert.IsTrue(reader.ReadBoolean());
            Assert.IsFalse(reader.ReadBoolean());
            Assert.IsTrue(reader.ReadBoolean());
            Assert.AreEqual(1234U, reader.ReadVariableLengthInteger());
            Assert.AreEqual(9876U, reader.ReadVariableLengthInteger());
        }
        
        [TestMethod]
        public void Reset_After_Usage()
        {
            var writer = new MqttPacketWriter();
            writer.WriteWithLengthPrefix("AString");
            writer.Write(0x1);
            writer.Write(0x0);
            writer.Write(0x1);
            writer.WriteVariableLengthInteger(1234U);
            writer.WriteVariableLengthInteger(9876U);

            writer.Reset(0);
            
            Assert.AreEqual(0, writer.Length);
        }
    }
}