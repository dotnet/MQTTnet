// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using MQTTnet.Formatter;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttApplicationMessageValidator_Tests
{
    [TestMethod]
    public void Succeed_When_Using_TopicAlias_And_MQTT_311()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
            MqttApplicationMessageValidator.ThrowIfNotSupported(new MqttApplicationMessageBuilder().WithTopicAlias(1).Build(), MqttProtocolVersion.V311));
    }

    [TestMethod]
    public void Succeed_When_Using_TopicAlias_And_MQTT_500()
    {
        MqttApplicationMessageValidator.ThrowIfNotSupported(new MqttApplicationMessageBuilder().WithTopicAlias(1).Build(), MqttProtocolVersion.V500);
    }

    [TestMethod]
    public void Succeed_When_Using_UserProperties_And_MQTT_500()
    {
        MqttApplicationMessageValidator.ThrowIfNotSupported(
            new MqttApplicationMessageBuilder().WithTopic("A").WithUserProperty("User", Encoding.UTF8.GetBytes("Property")).Build(),
            MqttProtocolVersion.V500);
    }

    [TestMethod]
    public void Succeed_When_Using_WillUserProperties_And_MQTT_311()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => MqttApplicationMessageValidator.ThrowIfNotSupported(
            new MqttApplicationMessageBuilder().WithTopic("B").WithUserProperty("User", Encoding.UTF8.GetBytes("Property")).Build(),
            MqttProtocolVersion.V311));
    }
}