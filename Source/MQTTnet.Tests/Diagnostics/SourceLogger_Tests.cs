// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Tests.Diagnostics;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class SourceLogger_Tests : BaseTestClass
{
    [TestMethod]
    public void Log_With_Source()
    {
        MqttNetLogMessage logMessage = null;

        var logger = new MqttNetEventLogger();
        logger.LogMessagePublished += (_, e) =>
        {
            logMessage = e.LogMessage;
        };

        var sourceLogger = logger.WithSource("The_Source");
        sourceLogger.Info("MESSAGE", (object)null, (object)null);

        Assert.AreEqual("The_Source", logMessage.Source);
    }
}