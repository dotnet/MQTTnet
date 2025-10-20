// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests;

public abstract class BaseTestClass
{
    public TestContext TestContext { get; set; }

    protected TestEnvironment CreateTestEnvironment(
        MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311, bool trackUnobservedTaskException = true)
    {
        return new TestEnvironment(TestContext, protocolVersion, trackUnobservedTaskException);
    }

    protected Task LongTestDelay()
    {
        return Task.Delay(TimeSpan.FromSeconds(1));
    }

    protected Task MediumTestDelay()
    {
        return Task.Delay(TimeSpan.FromSeconds(0.5));
    }
}