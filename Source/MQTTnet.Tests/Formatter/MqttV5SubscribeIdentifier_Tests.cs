// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V5;
using MQTTnet.Packets;

namespace MQTTnet.Tests.Formatter;

[TestClass]
public sealed class MqttV5SubscribeIdentifier_Tests
{
    [TestMethod]
    [DataRow("0B00")]
    [DataRow("0B010B01")]
    [DataRow("0B010B02")]
    [DataRow("0B0126000000000B02")]
    public void Reject_Invalid_Subscription_Identifier(string propertiesHex)
    {
        var properties = Convert.FromHexString(propertiesHex);
        byte[] packet = [0x82, (byte)(7 + properties.Length), 0, 1, (byte)properties.Length, .. properties, 0, 1, (byte)'a', 0];

        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500));
    }

    [TestMethod]
    [DataRow("", 0U)]
    [DataRow("0B01", 1U)]
    [DataRow("0BFFFFFF7F", 268435455U)]
    [DataRow("26000000000B012600000000", 1U)]
    public void Accept_Valid_Subscription_Identifier(string propertiesHex, uint expected)
    {
        var properties = Convert.FromHexString(propertiesHex);
        byte[] packet = [0x82, (byte)(7 + properties.Length), 0, 1, (byte)properties.Length, .. properties, 0, 1, (byte)'a', 0];
        var decoded = (MqttSubscribePacket)MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500);

        Assert.AreEqual(expected, decoded.SubscriptionIdentifier);
    }

    [TestMethod]
    public void Accept_Same_Identifier_In_Separate_Subscribe_Packets()
    {
        byte[] packet = [0x82, 9, 0, 1, 2, 0x0B, 1, 0, 1, (byte)'a', 0];
        var decoder = new MqttV5PacketDecoder();
        var received = new ReceivedMqttPacket(packet[0], new ArraySegment<byte>(packet, 2, packet.Length - 2), packet.Length);

        Assert.AreEqual(1U, ((MqttSubscribePacket)decoder.Decode(received)).SubscriptionIdentifier);
        Assert.AreEqual(1U, ((MqttSubscribePacket)decoder.Decode(received)).SubscriptionIdentifier);
    }

    [TestMethod]
    public void Accept_Repeated_Subscription_Identifiers_In_Publish()
    {
        byte[] packet = [0x30, 8, 0, 1, (byte)'a', 4, 0x0B, 1, 0x0B, 1];
        var decoded = (MqttPublishPacket)MqttPacketSerializationHelper.DecodePacket(packet, MqttProtocolVersion.V500);

        CollectionAssert.AreEqual(new uint[] { 1, 1 }, decoded.SubscriptionIdentifiers);
    }
}