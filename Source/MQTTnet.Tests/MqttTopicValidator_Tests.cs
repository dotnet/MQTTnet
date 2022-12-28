// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class MqttTopicValidator_Tests
    {
        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Empty()
        {
            MqttTopicValidator.ThrowIfInvalid(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Hash()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/#/c");
        }

        [TestMethod]
        [ExpectedException(typeof(MqttProtocolViolationException))]
        public void Invalid_Topic_Plus()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/+/c");
        }

        [TestMethod]
        public void Valid_Topic()
        {
            MqttTopicValidator.ThrowIfInvalid("/a/b/c");
        }
    }
}