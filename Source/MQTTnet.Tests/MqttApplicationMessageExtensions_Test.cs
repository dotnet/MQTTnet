// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace MQTTnet.Tests
{
    [TestClass]
    public sealed class MqttApplicationMessageExtensions_Test
    {
        [TestMethod]
        public void ConvertPayloadToJson_Test()
        {
            var input = new TValue { IntValue = 10, StrinValue = nameof(TValue) };

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("Abc")
                .WithPayload(JsonSerializer.SerializeToUtf8Bytes(input))
                .Build();

            var output = message.ConvertPayloadToJson<TValue>();

            Assert.AreEqual(input.IntValue, output.IntValue);
            Assert.AreEqual(input.StrinValue, output.StrinValue);
        }

        private class TValue
        {
            public int IntValue { get; set; }

            public string StrinValue { get; set; }
        }
    }
}
