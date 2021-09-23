using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Mockups
{
    public sealed class MqttPacketAsserts
    {
        public void AssertIsConnectPacket(MqttBasePacket packet)
        {
            Assert.AreEqual(packet.GetType(), typeof(MqttConnectPacket));
        }
        
        public void AssertIsConnAckPacket(MqttBasePacket packet)
        {
            Assert.AreEqual(packet.GetType(), typeof(MqttConnAckPacket));
        }
    }
}