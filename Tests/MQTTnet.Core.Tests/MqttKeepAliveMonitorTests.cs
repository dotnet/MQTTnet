using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
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
            var clientSession = new TestClientSession();
            var monitor = new MqttClientKeepAliveMonitor(clientSession, new MqttNetLogger().CreateChildLogger());

            Assert.AreEqual(0, clientSession.StopCalledCount);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, clientSession.StopCalledCount);

            Thread.Sleep(2000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.

            Assert.AreEqual(1, clientSession.StopCalledCount);
        }

        [TestMethod]
        public void KeepAlive_NoTimeout()
        {
            var clientSession = new TestClientSession();
            var monitor = new MqttClientKeepAliveMonitor(clientSession, new MqttNetLogger().CreateChildLogger());

            Assert.AreEqual(0, clientSession.StopCalledCount);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, clientSession.StopCalledCount);

            // Simulate traffic.
            Thread.Sleep(1000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.
            monitor.PacketReceived(new MqttPublishPacket());
            Thread.Sleep(1000);
            monitor.PacketReceived(new MqttPublishPacket());
            Thread.Sleep(1000);

            Assert.AreEqual(0, clientSession.StopCalledCount);

            Thread.Sleep(2000);

            Assert.AreEqual(1, clientSession.StopCalledCount);
        }

        private class TestClientSession : IMqttClientSession
        {
            public string ClientId { get; }

            public int StopCalledCount { get; private set; }

            public void FillStatus(MqttClientSessionStatus status)
            {
                throw new NotSupportedException();
            }

            public void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
            {
                throw new NotSupportedException();
            }

            public void ClearPendingApplicationMessages()
            {
                throw new NotSupportedException();
            }

            public Task RunAsync(MqttApplicationMessage willMessage, int keepAlivePeriod, IMqttChannelAdapter adapter)
            {
                throw new NotSupportedException();
            }

            public void Stop(MqttClientDisconnectType disconnectType)
            {
                StopCalledCount++;
            }

            public Task SubscribeAsync(IList<TopicFilter> topicFilters)
            {
                throw new NotSupportedException();
            }

            public Task UnsubscribeAsync(IList<string> topicFilters)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
