using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttPacketIdentifierProvider_Tests
    {
        [TestMethod]
        public void Reset()
        {
            var p = new MqttPacketIdentifierProvider();
            Assert.AreEqual(1, p.GetNextPacketIdentifier());
            Assert.AreEqual(2, p.GetNextPacketIdentifier());
            p.Reset();
            Assert.AreEqual(1, p.GetNextPacketIdentifier());
        }

        [TestMethod]
        public void ReachBoundaries()
        {
            var p = new MqttPacketIdentifierProvider();

            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                Assert.AreEqual(i + 1, p.GetNextPacketIdentifier());
            }

            Assert.AreEqual(1, p.GetNextPacketIdentifier());
        }
    }
}
