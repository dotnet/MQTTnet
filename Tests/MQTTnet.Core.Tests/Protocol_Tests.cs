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
                var buffer = MqttPacketWriter.EncodeVariableLengthInteger(i);
                var reader = new MqttPacketBodyReader(buffer.Array, buffer.Offset, buffer.Count);

                var checkValue = reader.ReadVariableLengthInteger();

                Assert.AreEqual(i, checkValue);
            }
        }
    }
}
