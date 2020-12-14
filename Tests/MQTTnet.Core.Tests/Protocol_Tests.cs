using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Protocol_Tests
    {
        [TestMethod]
        public void Encode_Four_Byte_Integer()
        {
            for (uint i = 0; i < 268435455; i++)
            {
                var writer = new MqttPacketWriter();
                writer.WriteVariableLengthInteger(i);
                var buffer = writer.GetBuffer();

                var reader = new MqttPacketBodyReader(buffer, 0, writer.Length);
                var checkValue = reader.ReadVariableLengthInteger();

                Assert.AreEqual(i, checkValue);
            }
        }
    }
}
