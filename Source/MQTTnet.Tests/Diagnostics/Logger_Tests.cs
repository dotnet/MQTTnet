// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.Diagnostics;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class Logger_Tests : BaseTestClass
{
    [TestMethod]
    public void Log_Without_Source()
    {
        var logger = new MqttNetEventLogger();

        MqttNetLogMessage logMessage = null;
        logger.LogMessagePublished += (_, e) => { logMessage = e.LogMessage; };

        logger.Publish(MqttNetLogLevel.Info, "SOURCE", "MESSAGE", ["ABC"], new InvalidOperationException());

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

        logger.LogMessagePublished += (_, _) => { logMessagesCount++; };

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

        logger.LogMessagePublished += (_, e) =>
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