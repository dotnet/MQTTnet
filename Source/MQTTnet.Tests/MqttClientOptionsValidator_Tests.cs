// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class MqttClientOptionsValidator_Tests
    {
        [TestMethod]
        public void Succeed_When_Using_UserProperties_And_MQTT_500()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500).WithUserProperty("User", "Property").WithTcpServer("FAKE").Build();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Succeed_When_Using_WillUserProperties_And_MQTT_311()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V311).WithWillUserProperty("User", "Property").WithTcpServer("FAKE").Build();
        }

        [TestMethod]
        public void Succeed_When_Using_WillUserProperties_And_MQTT_500()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500).WithWillUserProperty("User", "Property").WithTcpServer("FAKE").Build();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Throw_When_Using_UserProperties_And_MQTT_311()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V311).WithUserProperty("User", "Property").WithTcpServer("FAKE").Build();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Throw_When_Using_WithRequestResponseInformation_And_MQTT_311()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V311).WithRequestResponseInformation().WithTcpServer("FAKE").Build();
        }

        [TestMethod]
        public void Throw_When_Using_WithRequestResponseInformation_And_MQTT_500()
        {
            new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500).WithRequestResponseInformation().WithTcpServer("FAKE").Build();
        }
    }
}