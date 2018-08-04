using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Tests;
using MQTTnet.Serializer;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class MqttPacketSerializerTestsWithSpanBasedReader : MqttPacketSerializerTests
    {
        protected override IPacketBodyReader ReaderFactory(byte[] data)
        {
            var result = new SpanBasedMqttPacketBodyReader();
            result.SetBuffer(data);
            return result;
        }
    }
}
