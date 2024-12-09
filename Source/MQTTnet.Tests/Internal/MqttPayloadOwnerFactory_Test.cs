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
            var buffer = new byte[size];
            Random.Shared.NextBytes(buffer);

            await using var owner = MqttPayloadOwnerFactory.CreateSingleSegment(size, payload =>
            {
                buffer.AsSpan().CopyTo(payload.Span);
            });

            Assert.AreEqual(size, owner.Payload.Length);
            Assert.IsTrue(buffer.AsSpan().SequenceEqual(owner.Payload.ToArray()));
        }

        [TestMethod]
        public async Task CreateMultipleSegment_1x4K_Test()
        {
            await CreateMultipleSegmentTest(x4K: 1);
        }

        [TestMethod]
        public async Task CreateMultipleSegment_2x4K_Test()
        {
            await CreateMultipleSegmentTest(x4K: 2);
        }

        [TestMethod]
        public async Task CreateMultipleSegment_4x4K_Test()
        {
            await CreateMultipleSegmentTest(x4K: 4);
        }

        [TestMethod]
        public async Task CreateMultipleSegment_8x4K_Test()
        {
            await CreateMultipleSegmentTest(x4K: 8);
        }

        private async Task CreateMultipleSegmentTest(int x4K)
        {
            const int size4K = 4096;
            var stream = new MemoryStream();
            await using var owner = await MqttPayloadOwnerFactory.CreateMultipleSegmentAsync(async writer =>
            {
                for (var i = 0; i < x4K; i++)
                {
                    var memory = writer.GetMemory(size4K)[..size4K];
                    Random.Shared.NextBytes(memory.Span);

                    writer.Advance(memory.Length);
                    await stream.WriteAsync(memory);
                }
                await writer.FlushAsync();
            });

            var buffer = stream.ToArray();
            var buffer2 = owner.Payload.ToArray();

            Assert.AreEqual(size4K * x4K, buffer.Length);
            Assert.IsTrue(buffer.SequenceEqual(buffer2));
        }
    }
}
