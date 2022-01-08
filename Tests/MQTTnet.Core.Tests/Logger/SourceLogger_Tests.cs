using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.Logger
{
    [TestClass]
    public sealed class SourceLogger_Tests : BaseTestClass
    {
        [TestMethod]
        public void Log_With_Source()
        {
            MqttNetLogMessage logMessage = null;
            
            var logger = new MqttNetEventLogger();
            logger.LogMessagePublished += (s, e) =>
            {
                logMessage = e.LogMessage;
            };
            
            var sourceLogger = logger.WithSource("The_Source");
            sourceLogger.Info("MESSAGE", (object)null, (object)null);
         
            Assert.AreEqual("The_Source", logMessage.Source);
        }
    }
}