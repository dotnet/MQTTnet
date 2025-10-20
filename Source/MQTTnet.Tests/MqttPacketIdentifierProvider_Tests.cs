// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MQTTnet.Tests;

// ReSharper disable InconsistentNaming
[TestClass]
public class MqttPacketIdentifierProvider_Tests
{
    [TestMethod]
    public void Reset()
    {
        var p = new MqttPacketIdentifierProvider();
        Assert.AreEqual(1, p.GetNextPacketIdentifier());
        Assert.AreEqual(2, p.GetNextPacketIdentifier());
        p.Reset();
        Assert.AreEqual(1, p.GetNextPacketIdentifier());
    }

    [TestMethod]
    public void ReachBoundaries()
    {
        var p = new MqttPacketIdentifierProvider();

        for (ushort i = 0; i < ushort.MaxValue; i++)
        {
            Assert.AreEqual(i + 1, p.GetNextPacketIdentifier());
        }

        Assert.AreEqual(1, p.GetNextPacketIdentifier());
    }
}