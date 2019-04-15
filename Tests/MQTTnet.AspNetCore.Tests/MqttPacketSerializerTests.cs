using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Tests;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class MqttPacketSerializerTestsWithSpanBasedReader : MqttPacketSerializer_Tests
    {
        protected override IMqttPacketBodyReader ReaderFactory(byte[] data)
        {
            var result = new SpanBasedMqttPacketBodyReader();
            result.SetBuffer(data);
            return result;
        }

        protected override IMqttPacketWriter WriterFactory()
        {
            return new SpanBasedMqttPacketWriter();
        }
    }
}
