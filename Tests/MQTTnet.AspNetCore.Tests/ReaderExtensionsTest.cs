#if NETCOREAPP
using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
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

            var buffer = serializer.Encode(new MqttPublishPacket() {Topic = "a", Payload = new byte[5]});

            var sequence = new ReadOnlySequence<byte>(buffer.Array, buffer.Offset, buffer.Count);

            var part = sequence;
            MqttBasePacket packet;
            var consumed = part.Start;
            var observed = part.Start;
            var result = false;

            var reader = new SpanBasedMqttPacketBodyReader();
            var received = new ReceivedMqttPacket(0, reader, 0);


            part = sequence.Slice(sequence.Start, 0); // empty message should fail
            reader.SetBuffer(part.ToArray());
            result = serializer.TryDecode(reader, received, part, out packet, out consumed, out observed);
            Assert.IsFalse(result);


            part = sequence.Slice(sequence.Start, 1); // partial fixed header should fail
            reader.SetBuffer(part.ToArray());
            result = serializer.TryDecode(reader, received, part, out packet, out consumed, out observed);
            Assert.IsFalse(result);

            part = sequence.Slice(sequence.Start, 4); // partial body should fail
            reader.SetBuffer(part.ToArray());
            result = serializer.TryDecode(reader, received, part, out packet, out consumed, out observed);
            Assert.IsFalse(result);

            part = sequence; // complete msg should work
            reader.SetBuffer(part.ToArray());
            result = serializer.TryDecode(reader, received, part, out packet, out consumed, out observed);
            Assert.IsTrue(result);
        }
    }
}
#endif