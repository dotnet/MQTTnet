using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests
{
    public abstract class BaseTestClass
    {
        public TestContext TestContext { get; set; }
        
        protected TestEnvironment CreateTestEnvironment()
        {
            return new TestEnvironment(TestContext);
        }

        protected Task LongDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}