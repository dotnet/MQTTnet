using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client.Receiving;
using MQTTnet.Implementations;

namespace MQTTnet.Tests.Mockups
{
    public sealed class TestApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        readonly List<MqttApplicationMessage> _messageReceivedEventArgs = new List<MqttApplicationMessage>();

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            lock (_messageReceivedEventArgs)
            {
                _messageReceivedEventArgs.Add(eventArgs.ApplicationMessage);
            }
            
            return PlatformAbstractionLayer.CompletedTask;
        }

        public List<MqttApplicationMessage> ReceivedApplicationMessages
        {
            get
            {
                lock (_messageReceivedEventArgs)
                {
                    return _messageReceivedEventArgs.ToList();
                }
            }
        }

        public void AssertReceivedCountEquals(int expectedCount)
        {
            lock (_messageReceivedEventArgs)
            {
                Assert.AreEqual(_messageReceivedEventArgs.Count, expectedCount);
            }
        }
    }
}