using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Tests;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class SpanBasedMqttPacketWriterTests : MqttPacketWriter_Tests
    {
        protected override IMqttPacketWriter WriterFactory()
        {
            return new SpanBasedMqttPacketWriter();
        }
    }
}
