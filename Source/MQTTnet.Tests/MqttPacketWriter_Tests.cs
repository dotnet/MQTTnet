// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public class MqttPacketWriter_Tests
{
    protected virtual MqttBufferWriter WriterFactory()
    {
        return new MqttBufferWriter(4096, 65535);
    }

    [TestMethod]
    public void WritePacket()
    {
        var writer = WriterFactory();
        Assert.AreEqual(0, writer.Length);

        writer.WriteString("1234567890");
        Assert.AreEqual(10 + 2, writer.Length);

        writer.WriteBinary(new byte[300]);
        Assert.AreEqual(300 + 2 + 12, writer.Length);

        writer.WriteBinary(new byte[5000]);
        Assert.AreEqual(5000 + 2 + 300 + 2 + 12, writer.Length);
    }
}