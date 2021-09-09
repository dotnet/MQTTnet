using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    public abstract class BaseTestClass
    {
        public TestContext TestContext { get; set; }
        
        protected TestEnvironment CreateTestEnvironment(MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            return new TestEnvironment(TestContext, protocolVersion);
        }

        protected Task LongTestDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}