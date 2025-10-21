// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttTopicValidator_Tests
{
    [TestMethod]
    public void Invalid_Topic_Empty()
    {
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttTopicValidator.ThrowIfInvalid(string.Empty));
    }

    [TestMethod]
    public void Invalid_Topic_Hash()
    {
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttTopicValidator.ThrowIfInvalid("/a/#/c"));
    }

    [TestMethod]
    public void Invalid_Topic_Plus()
    {
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttTopicValidator.ThrowIfInvalid("/a/+/c"));
    }

    [TestMethod]
    public void Valid_Topic()
    {
        MqttTopicValidator.ThrowIfInvalid("/a/b/c");
    }
}