// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Tests
{
    public abstract class BaseTestClass
    {
        public TestContext TestContext { get; set; }

        protected TestEnvironmentCollection CreateMQTTnetTestEnvironment(MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var mqttnet = new TestEnvironment(TestContext, protocolVersion);
            return new TestEnvironmentCollection(mqttnet);
        }

        protected TestEnvironmentCollection CreateAspNetCoreTestEnvironment(MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var aspnetcore = new AspNetCoreTestEnvironment(TestContext, protocolVersion);
            return new TestEnvironmentCollection(aspnetcore);
        }

        protected TestEnvironmentCollection CreateMixedTestEnvironment(MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var mqttnet = new TestEnvironment(TestContext, protocolVersion);
            var aspnetcore = new AspNetCoreTestEnvironment(TestContext, protocolVersion);
            return new TestEnvironmentCollection(mqttnet, aspnetcore);
        }

        protected Task LongTestDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}