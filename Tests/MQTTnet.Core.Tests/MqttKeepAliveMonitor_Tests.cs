using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using MQTTnet.Server.Status;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttKeepAliveMonitor_Tests
    {
        [TestMethod]
        public void KeepAlive_Timeout()
        {
            var counter = 0;

            var monitor = new MqttClientKeepAliveMonitor("", () =>
                {
                    counter++;
                    return Task.CompletedTask;
                },
                new MqttNetLogger().CreateChildLogger());

            Assert.AreEqual(0, counter);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, counter);

            Thread.Sleep(2000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.

            Assert.AreEqual(1, counter);
        }

        [TestMethod]
        public void KeepAlive_NoTimeout()
        {
            var counter = 0;

            var monitor = new MqttClientKeepAliveMonitor("", () =>
                {
                    counter++;
                    return Task.CompletedTask;
                },
                new MqttNetLogger().CreateChildLogger());

            Assert.AreEqual(0, counter);

            monitor.Start(1, CancellationToken.None);

            Assert.AreEqual(0, counter);

            // Simulate traffic.
            Thread.Sleep(1000); // Internally the keep alive timeout is multiplied with 1.5 as per protocol specification.
            monitor.PacketReceived();
            Thread.Sleep(1000);
            monitor.PacketReceived();
            Thread.Sleep(1000);

            Assert.AreEqual(0, counter);

            Thread.Sleep(2000);

            Assert.AreEqual(1, counter);
        }

        private class TestClientSession : IMqttClientSession
        {
            public string ClientId { get; }

            public int StopCalledCount { get; private set; }

            public void FillStatus(MqttClientStatus status)
            {
                throw new NotSupportedException();
            }

            public void EnqueueApplicationMessage(MqttClientConnection senderClientSession, MqttApplicationMessage applicationMessage)
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

            public Task StopAsync()
            {
                StopCalledCount++;
                return Task.FromResult(0);
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
