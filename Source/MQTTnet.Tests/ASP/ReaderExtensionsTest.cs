// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Tests.ASP;

[TestClass]
public sealed class ReaderExtensionsTest
{
    [TestMethod]
    public void TestTryDeserialize()
    {
        var serializer = new MqttPacketFormatterAdapter(MqttProtocolVersion.V311, new MqttBufferWriter(4096, 65535));

        var buffer = serializer.Encode(new MqttPublishPacket { Topic = "a", PayloadSequence = new ReadOnlySequence<byte>(new byte[5]) }).Join();

        var sequence = new ReadOnlySequence<byte>(buffer.Array, buffer.Offset, buffer.Count);

        var part = sequence;
        var consumed = part.Start;
        var observed = part.Start;
        var read = 0;

        part = sequence.Slice(sequence.Start, 0); // empty message should fail
        var result = serializer.TryDecode(part, out _, out consumed, out observed, out read);
        Assert.IsFalse(result);

        part = sequence.Slice(sequence.Start, 1); // partial fixed header should fail
        result = serializer.TryDecode(part, out _, out consumed, out observed, out read);
        Assert.IsFalse(result);

        part = sequence.Slice(sequence.Start, 4); // partial body should fail
        result = serializer.TryDecode(part, out _, out consumed, out observed, out read);
        Assert.IsFalse(result);

        part = sequence; // complete msg should work
        result = serializer.TryDecode(part, out _, out consumed, out observed, out read);
        Assert.IsTrue(result);
    }
}