using Microsoft.VisualStudio.TestTools .UnitTesting;
using MQTTnet.Client;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketIdentifierProviderTests
    {
        [TestMethod]
        public void Reset()
        {
            var p = new MqttPacketIdentifierProvider();
            Assert.AreEqual(1, p.GetNewPacketIdentifier());
            Assert.AreEqual(2, p.GetNewPacketIdentifier());
            p.Reset();
            Assert.AreEqual(1, p.GetNewPacketIdentifier());
        }

        [TestMethod]
        public void ReachBoundaries()
        {
            var p = new MqttPacketIdentifierProvider();

            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                Assert.AreEqual(i + 1, p.GetNewPacketIdentifier());
            }

            Assert.AreEqual(1, p.GetNewPacketIdentifier());
        }
    }
}
