using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Diagnostics;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttNetLogger_Tests
    {
        [TestMethod]
        public void Root_Log_Messages()
        {
            var logger = new MqttNetLogger();
            var childLogger = logger.CreateScopedLogger("Source1");

            var logMessagesCount = 0;

            logger.LogMessagePublished += (s, e) =>
            {
                logMessagesCount++;
            };

            childLogger.Verbose("Verbose");
            childLogger.Info("Info");
            childLogger.Warning(null, "Warning");
            childLogger.Error(null, "Error");

            Assert.AreEqual(4, logMessagesCount);
        }

        [TestMethod]
        public void Bubbling_Log_Messages()
        {
            var logger = new MqttNetLogger();
            var childLogger = logger.CreateScopedLogger("Source1");

            var logMessagesCount = 0;

            logger.LogMessagePublished += (s, e) =>
            {
                logMessagesCount++;
            };

            childLogger.Verbose("Verbose");
            childLogger.Info("Info");
            childLogger.Warning(null, "Warning");
            childLogger.Error(null, "Error");

            Assert.AreEqual(4, logMessagesCount);
        }

        [TestMethod]
        public void Set_Custom_Log_ID()
        {
            var logger = new MqttNetLogger("logId");
            var childLogger = logger.CreateScopedLogger("Source1");

            logger.LogMessagePublished += (s, e) =>
            {
                Assert.AreEqual("logId", e.LogMessage.LogId);
            };

            childLogger.Verbose("Verbose");
            childLogger.Info("Info");
            childLogger.Warning(null, "Warning");
            childLogger.Error(null, "Error");
        }
    }
}
