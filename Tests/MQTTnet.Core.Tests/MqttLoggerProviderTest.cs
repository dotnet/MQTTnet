using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttLoggerProviderTest
    {
        [TestMethod]
        public void TestLoggerCallback()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            using ((IDisposable)serviceProvider)
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                loggerFactory.AddMqttTrace();

                var expectedMsg = "Hello World!";
                MqttNetTraceMessage msg = null;

                MqttNetTrace.TraceMessagePublished += (sender, args) =>
                {
                    msg = args.TraceMessage;
                };

                var logger = loggerFactory.CreateLogger<MqttLoggerProviderTest>();

                logger.LogInformation(expectedMsg);

                Assert.AreEqual(expectedMsg, msg.Message);

                var expectedException = new Exception("bad stuff");

                logger.LogError(new EventId(), expectedException, expectedException.Message);
                
                Assert.AreEqual(expectedException, msg.Exception);
                Assert.AreEqual(expectedException.Message, msg.Message);
            }
        }
    }
}
