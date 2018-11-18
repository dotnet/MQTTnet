#if NETCOREAPP
using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter.V311;
using MQTTnet.Packets;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class ReaderExtensionsTest
    {
        [TestMethod]
        public void TestTryDeserialize()
        {
            var serializer = new MqttV311PacketFormatter();

            var buffer = serializer.Encode(new MqttPublishPacket() {Topic = "a", Payload = new byte[5]});

            var sequence = new ReadOnlySequence<byte>(buffer.Array, buffer.Offset, buffer.Count);

            var part = sequence;
            MqttBasePacket packet;
            var consumed = part.Start;
            var observed = part.Start;
            var result = false;

            part = sequence.Slice(sequence.Start, 0); // empty message should fail
            result = serializer.TryDeserialize(part, out packet, out consumed, out observed);
            Assert.IsFalse(result);


            part = sequence.Slice(sequence.Start, 1); // partial fixed header should fail
            result = serializer.TryDeserialize(part, out packet, out consumed, out observed);
            Assert.IsFalse(result);

            part = sequence.Slice(sequence.Start, 4); // partial body should fail
            result = serializer.TryDeserialize(part, out packet, out consumed, out observed);
            Assert.IsFalse(result);

            part = sequence; // complete msg should work
            result = serializer.TryDeserialize(part, out packet, out consumed, out observed);
            Assert.IsTrue(result);
        }
    }
}
#endif