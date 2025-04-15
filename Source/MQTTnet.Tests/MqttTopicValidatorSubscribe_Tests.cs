// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public class MqttTopicValidatorSubscribe_Tests
{
    [TestMethod]
    public void Valid_Topic()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("/a/b/c");
    }

    [TestMethod]
    public void Valid_Topic_Plus_In_Between()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("/a/+/c");
    }

    [TestMethod]
    public void Valid_Topic_Plus_Last_Char()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("/a/+");
    }

    [TestMethod]
    public void Valid_Topic_Hash_Last_Char()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("/a/#");
    }

    [TestMethod]
    public void Valid_Topic_Only_Hash()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("#");
    }

    [TestMethod]
    [ExpectedException(typeof(MqttProtocolViolationException))]
    public void Invalid_Topic_Hash_In_Between()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe("/a/#/c");
    }

    [TestMethod]
    [ExpectedException(typeof(MqttProtocolViolationException))]
    public void Invalid_Topic_Empty()
    {
        MqttTopicValidator.ThrowIfInvalidSubscribe(string.Empty);
    }
}