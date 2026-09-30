// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Formatter;

[TestClass]
public sealed class MqttV5PublishPropertyValidation_Tests
{
    [TestMethod]
    [DataRow("0100")]
    [DataRow("0200000000")]
    [DataRow("030000")]
    [DataRow("08000161")]
    [DataRow("090000")]
    [DataRow("230001")]
    public void Publish_Rejects_Duplicate_Singleton_Property(string propertyHex)
    {
        var property = Convert.FromHexString(propertyHex);
        byte[] packet = [0x30, (byte)(4 + 2 * property.Length), 0, 1, (byte)'a', (byte)(2 * property.Length), .. property, .. property];

        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500));
    }

    [TestMethod]
    public void Publish_Allows_Repeated_Subscription_Identifiers_And_User_Properties()
    {
        byte[] packet = [0x30, 18, 0, 1, (byte)'a', 14, 0x0B, 1, 0x26, 0, 0, 0, 0, 0x0B, 1, 0x26, 0, 0, 0, 0];
        var decoded = (MqttPublishPacket)MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500);

        CollectionAssert.AreEqual(new uint[] { 1, 1 }, decoded.SubscriptionIdentifiers);
        Assert.HasCount(2, decoded.UserProperties);
    }


}