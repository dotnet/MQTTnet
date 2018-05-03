using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttKeepAliveMonitorTests
    {
        [TestMethod]
        public void KeepAlive_Timeout()
        {
            var timeoutCalledCount = 0;

            var monitor = new MqttClientKeepAliveMonitor(string.Empty, delegate
            {
                timeoutCalledCount++;
            }, new MqttNetLogger());

            Assert.AreEqual(0, timeoutCalledCount);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, timeoutCalledCount);

            Thread.Sleep(2000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.

            Assert.AreEqual(1, timeoutCalledCount);
        }

        [TestMethod]
        public void KeepAlive_NoTimeout()
        {
            var timeoutCalledCount = 0;

            var monitor = new MqttClientKeepAliveMonitor(string.Empty, delegate
            {
                timeoutCalledCount++;
            }, new MqttNetLogger());

            Assert.AreEqual(0, timeoutCalledCount);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, timeoutCalledCount);

            // Simulate traffic.
            Thread.Sleep(1000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.
            monitor.PacketReceived(new MqttPublishPacket());
            Thread.Sleep(1000);
            monitor.PacketReceived(new MqttPublishPacket());
            Thread.Sleep(1000);

            Assert.AreEqual(0, timeoutCalledCount);

            Thread.Sleep(2000);

            Assert.AreEqual(1, timeoutCalledCount);
        }
    }
}
