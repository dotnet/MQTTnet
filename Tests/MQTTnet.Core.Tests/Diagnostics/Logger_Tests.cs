using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests.Diagnostics
{
    [TestClass]
    public sealed class Logger_Tests : BaseTestClass
    {
        [TestMethod]
        public void Log_Without_Source()
        {
            var logger = new MqttNetEventLogger();

            MqttNetLogMessage logMessage = null;
            logger.LogMessagePublished += (s, e) => { logMessage = e.LogMessage; };

            logger.Publish(MqttNetLogLevel.Info, "SOURCE", "MESSAGE", new object[] { "ABC" }, new InvalidOperationException());

            Assert.AreEqual(MqttNetLogLevel.Info, logMessage.Level);
            Assert.AreEqual("SOURCE", logMessage.Source);
            Assert.AreEqual("MESSAGE", logMessage.Message);
            Assert.AreEqual("InvalidOperationException", logMessage.Exception.GetType().Name);
        }

        [TestMethod]
        public void Root_Log_Messages()
        {
            var logger = new MqttNetEventLogger();
            var childLogger = logger.WithSource("Source1");

            var logMessagesCount = 0;

            logger.LogMessagePublished += (s, e) => { logMessagesCount++; };

            childLogger.Verbose("Verbose");
            childLogger.Info("Info");
            childLogger.Warning((Exception)null, "Warning");
            childLogger.Error(null, "Error");

            Assert.AreEqual(4, logMessagesCount);
        }
        
        [TestMethod]
        public void Use_Custom_Log_Id()
        {
            var logger = new MqttNetEventLogger("logId");
            var childLogger = logger.WithSource("Source1");

            logger.LogMessagePublished += (s, e) =>
            {
                Assert.AreEqual("logId", e.LogMessage.LogId);
                Assert.AreEqual("Source1", e.LogMessage.Source);
            };
            
            childLogger.Verbose("Verbose");
            childLogger.Info("Info");
            childLogger.Warning((Exception)null, "Warning");
            childLogger.Error(null, "Error");
        }
    }
}