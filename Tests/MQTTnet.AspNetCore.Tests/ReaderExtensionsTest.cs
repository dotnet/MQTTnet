#if NETCOREAPP3_1
using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore.Extensions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class ReaderExtensionsTest
    {
        [TestMethod]
        public void TestTryDeserialize()
        {
            var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311);

            var buffer = serializer.Encode(new MqttPublishPacket {Topic = "a", Payload = new byte[5]}).ToArray();

            var sequence = new ReadOnlySequence<byte>(buffer.Array, buffer.Offset, buffer.Count);

            var part = sequence;
            MqttBasePacket packet;
            var consumed = part.Start;
            var observed = part.Start;
            var result = false;
            var read = 0;

            var reader = new SpanBasedMqttPacketBodyReader();

            part = sequence.Slice(sequence.Start, 0); // empty message should fail
            result = serializer.TryDecode(reader, part, out packet, out consumed, out observed, out read);
            Assert.IsFalse(result);

            part = sequence.Slice(sequence.Start, 1); // partial fixed header should fail
            result = serializer.TryDecode(reader, part, out packet, out consumed, out observed, out read);
            Assert.IsFalse(result);

            part = sequence.Slice(sequence.Start, 4); // partial body should fail
            result = serializer.TryDecode(reader, part, out packet, out consumed, out observed, out read);
            Assert.IsFalse(result);

            part = sequence; // complete msg should work
            result = serializer.TryDecode(reader, part, out packet, out consumed, out observed, out read);
            Assert.IsTrue(result);
        }
    }
}
#endif