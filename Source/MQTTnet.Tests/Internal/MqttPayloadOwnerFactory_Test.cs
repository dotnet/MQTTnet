// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public class MqttPayloadOwnerFactory_Test
    {
        [TestMethod]
        public async Task CreateSingleSegmentTest()
        {
            var size = 10;
            await using var owner = MqttPayloadOwnerFactory.CreateSingleSegment(size, out var payloadMemory);
            Random.Shared.NextBytes(payloadMemory.Span);

            Assert.AreEqual(size, owner.Payload.Length);
            Assert.IsTrue(payloadMemory.Span.SequenceEqual(owner.Payload.ToArray()));
        }

        [TestMethod]
        public async Task CreateMultipleSegmentTest()
        {
            var stream = new MemoryStream();
            await using var owner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(async writer =>
            {
                for (var i = 0; i < 10; i++)
                {
                    var memory = writer.GetMemory(4096);
                    Random.Shared.NextBytes(memory.Span);

                    writer.Advance(memory.Length);
                    await stream.WriteAsync(memory);
                }
                await writer.FlushAsync();
            });

            var buffer = stream.ToArray();
            var buffer2 = owner.Payload.ToArray();
            Assert.IsTrue(buffer.SequenceEqual(buffer2));
        }
    }
}
