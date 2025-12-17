// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Formatter;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class MqttPacketSerialization_V3_Tests
{
    [TestMethod]
    public void Serialize_Full_MqttAuthPacket_V311()
    {
        var authPacket = new MqttAuthPacket();
        Assert.ThrowsExactly<MqttProtocolViolationException>(() => MqttPacketSerializationHelper.EncodeAndDecodePacket(authPacket, MqttProtocolVersion.V311));
    }

    [TestMethod]
    public void Serialize_Full_MqttConnAckPacket_V311()
    {
        var connAckPacket = new MqttConnAckPacket
        {
            AuthenticationData = "AuthenticationData"u8.ToArray(),
            AuthenticationMethod = "AuthenticationMethod",
            ReasonCode = MqttConnectReasonCode.ServerUnavailable,
            ReasonString = "ReasonString",
            ReceiveMaximum = 123,
            ResponseInformation = "ResponseInformation",
            RetainAvailable = true,
            ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized,
            ServerReference = "ServerReference",
            AssignedClientIdentifier = "AssignedClientIdentifier",
            IsSessionPresent = true,
            MaximumPacketSize = 456,
            MaximumQoS = MqttQualityOfServiceLevel.ExactlyOnce,
            ServerKeepAlive = 789,
            SessionExpiryInterval = 852,
            SharedSubscriptionAvailable = true,
            SubscriptionIdentifiersAvailable = true,
            TopicAliasMaximum = 963,
            WildcardSubscriptionAvailable = true,
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(connAckPacket, MqttProtocolVersion.V311);

        CollectionAssert.AreEqual(null, deserialized.AuthenticationData); // Not supported in v3.1.1
        Assert.IsNull(deserialized.AuthenticationMethod); // Not supported in v3.1.1
        //Assert.AreEqual(connAckPacket.ReasonCode, deserialized.ReasonCode);
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.ReceiveMaximum); // Not supported in v3.1.1
        Assert.IsNull(deserialized.ResponseInformation); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.RetainAvailable); // Not supported in v3.1.1
        Assert.AreEqual(MqttConnectReturnCode.ConnectionRefusedNotAuthorized, deserialized.ReturnCode);
        Assert.IsNull(deserialized.ServerReference); // Not supported in v3.1.1
        Assert.IsNull(deserialized.AssignedClientIdentifier); // Not supported in v3.1.1
        Assert.AreEqual(connAckPacket.IsSessionPresent, deserialized.IsSessionPresent);
        Assert.AreEqual(0U, deserialized.MaximumPacketSize); // Not supported in v3.1.1
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, deserialized.MaximumQoS); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.ServerKeepAlive); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.SessionExpiryInterval); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.SharedSubscriptionAvailable); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.SubscriptionIdentifiersAvailable); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.TopicAliasMaximum); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.WildcardSubscriptionAvailable);
        Assert.IsNull(deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttConnAckPacket_V310()
    {
        var connAckPacket = new MqttConnAckPacket
        {
            AuthenticationData = "AuthenticationData"u8.ToArray(),
            AuthenticationMethod = "AuthenticationMethod",
            ReasonCode = MqttConnectReasonCode.ServerUnavailable,
            ReasonString = "ReasonString",
            ReceiveMaximum = 123,
            ResponseInformation = "ResponseInformation",
            RetainAvailable = true,
            ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized,
            ServerReference = "ServerReference",
            AssignedClientIdentifier = "AssignedClientIdentifier",
            IsSessionPresent = true,
            MaximumPacketSize = 456,
            MaximumQoS = MqttQualityOfServiceLevel.ExactlyOnce,
            ServerKeepAlive = 789,
            SessionExpiryInterval = 852,
            SharedSubscriptionAvailable = true,
            SubscriptionIdentifiersAvailable = true,
            TopicAliasMaximum = 963,
            WildcardSubscriptionAvailable = true,
            UserProperties = [new MqttUserProperty("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(connAckPacket, MqttProtocolVersion.V310);

        CollectionAssert.AreEqual(null, deserialized.AuthenticationData); // Not supported in v3.1.1
        Assert.IsNull(deserialized.AuthenticationMethod); // Not supported in v3.1.1
        //Assert.AreEqual(connAckPacket.ReasonCode, deserialized.ReasonCode);
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.ReceiveMaximum); // Not supported in v3.1.1
        Assert.IsNull(deserialized.ResponseInformation); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.RetainAvailable); // Not supported in v3.1.1
        Assert.AreEqual(MqttConnectReturnCode.ConnectionRefusedNotAuthorized, deserialized.ReturnCode);
        Assert.IsNull(deserialized.ServerReference); // Not supported in v3.1.1
        Assert.IsNull(deserialized.AssignedClientIdentifier); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.IsSessionPresent); // Not supported in v3.1.0 <- !
        Assert.AreEqual(0U, deserialized.MaximumPacketSize); // Not supported in v3.1.1
        Assert.AreEqual(MqttQualityOfServiceLevel.AtMostOnce, deserialized.MaximumQoS); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.ServerKeepAlive); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.SessionExpiryInterval); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.SharedSubscriptionAvailable); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.SubscriptionIdentifiersAvailable); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.TopicAliasMaximum); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.WildcardSubscriptionAvailable);
        Assert.IsNull(deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttConnectPacket_V311()
    {
        var connectPacket = new MqttConnectPacket
        {
            Username = "Username",
            Password = "Password"u8.ToArray(),
            ClientId = "ClientId",
            AuthenticationData = "AuthenticationData"u8.ToArray(),
            AuthenticationMethod = "AuthenticationMethod",
            CleanSession = true,
            ReceiveMaximum = 123,
            WillFlag = true,
            WillTopic = "WillTopic",
            WillMessage = "WillMessage"u8.ToArray(),
            WillRetain = true,
            KeepAlivePeriod = 456,
            MaximumPacketSize = 789,
            RequestProblemInformation = true,
            RequestResponseInformation = true,
            SessionExpiryInterval = 27,
            TopicAliasMaximum = 67,
            WillContentType = "WillContentType",
            WillCorrelationData = "WillCorrelationData"u8.ToArray(),
            WillDelayInterval = 782,
            WillQoS = MqttQualityOfServiceLevel.ExactlyOnce,
            WillResponseTopic = "WillResponseTopic",
            WillMessageExpiryInterval = 542,
            WillPayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData,
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))],
            WillUserProperties = [new("WillFoo", Encoding.UTF8.GetBytes("WillBar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(connectPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(connectPacket.Username, deserialized.Username);
        CollectionAssert.AreEqual(connectPacket.Password, deserialized.Password);
        Assert.AreEqual(connectPacket.ClientId, deserialized.ClientId);
        CollectionAssert.AreEqual(null, deserialized.AuthenticationData); // Not supported in v3.1.1
        Assert.IsNull(deserialized.AuthenticationMethod); // Not supported in v3.1.1
        Assert.AreEqual(connectPacket.CleanSession, deserialized.CleanSession);
        Assert.AreEqual(0L, deserialized.ReceiveMaximum); // Not supported in v3.1.1
        Assert.AreEqual(connectPacket.WillFlag, deserialized.WillFlag);
        Assert.AreEqual(connectPacket.WillTopic, deserialized.WillTopic);
        CollectionAssert.AreEqual(connectPacket.WillMessage, deserialized.WillMessage);
        Assert.AreEqual(connectPacket.WillRetain, deserialized.WillRetain);
        Assert.AreEqual(connectPacket.KeepAlivePeriod, deserialized.KeepAlivePeriod);
        // MaximumPacketSize not available in MQTTv3.
        // RequestProblemInformation not available in MQTTv3.
        // RequestResponseInformation not available in MQTTv3.
        // SessionExpiryInterval not available in MQTTv3.
        // TopicAliasMaximum not available in MQTTv3.
        // WillContentType not available in MQTTv3.
        // WillCorrelationData not available in MQTTv3.
        // WillDelayInterval not available in MQTTv3.
        Assert.AreEqual(connectPacket.WillQoS, deserialized.WillQoS);
        // WillResponseTopic not available in MQTTv3.
        // WillMessageExpiryInterval not available in MQTTv3.
        // WillPayloadFormatIndicator not available in MQTTv3.
        Assert.IsNull(deserialized.UserProperties); // Not supported in v3.1.1
        Assert.IsNull(deserialized.WillUserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttDisconnectPacket_V311()
    {
        var disconnectPacket = new MqttDisconnectPacket
        {
            ReasonCode = MqttDisconnectReasonCode.NormalDisconnection, // MQTTv3 has no other values than this.
            ReasonString = "ReasonString",
            ServerReference = "ServerReference",
            SessionExpiryInterval = 234,
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(disconnectPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(disconnectPacket.ReasonCode, deserialized.ReasonCode);
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        Assert.IsNull(deserialized.ServerReference); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.SessionExpiryInterval); // Not supported in v3.1.1
        CollectionAssert.AreEqual(null, deserialized.UserProperties);
    }

    [TestMethod]
    public void Serialize_Full_MqttPingReqPacket_V311()
    {
        var pingReqPacket = new MqttPingReqPacket();

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pingReqPacket, MqttProtocolVersion.V311);

        Assert.IsNotNull(deserialized);
    }

    [TestMethod]
    public void Serialize_Full_MqttPingRespPacket_V311()
    {
        var pingRespPacket = new MqttPingRespPacket();

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pingRespPacket, MqttProtocolVersion.V311);

        Assert.IsNotNull(deserialized);
    }

    [TestMethod]
    public void Serialize_Full_MqttPubAckPacket_V311()
    {
        var pubAckPacket = new MqttPubAckPacket
        {
            PacketIdentifier = 123,
            ReasonCode = MqttPubAckReasonCode.NoMatchingSubscribers,
            ReasonString = "ReasonString",
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubAckPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(pubAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.AreEqual(MqttPubAckReasonCode.Success, deserialized.ReasonCode); // Not supported in v3.1.1
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        CollectionAssert.AreEqual(null, deserialized.UserProperties);
    }

    [TestMethod]
    public void Serialize_Full_MqttPubCompPacket_V311()
    {
        var pubCompPacket = new MqttPubCompPacket
        {
            PacketIdentifier = 123,
            ReasonCode = MqttPubCompReasonCode.PacketIdentifierNotFound,
            ReasonString = "ReasonString",
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubCompPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(pubCompPacket.PacketIdentifier, deserialized.PacketIdentifier);
        // ReasonCode not available in MQTTv3.
        // ReasonString not available in MQTTv3.
        // UserProperties not available in MQTTv3.
        Assert.IsNull(deserialized.UserProperties);
    }

    [TestMethod]
    public void Serialize_Full_MqttPublishPacket_V311()
    {
        var publishPacket = new MqttPublishPacket
        {
            PacketIdentifier = 123,
            Dup = true,
            Retain = true,
            PayloadSegment = new ArraySegment<byte>("Payload"u8.ToArray()),
            QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
            Topic = "Topic",
            ResponseTopic = "/Response",
            ContentType = "Content-Type",
            CorrelationData = "CorrelationData"u8.ToArray(),
            TopicAlias = 27,
            SubscriptionIdentifiers = [123],
            MessageExpiryInterval = 38,
            PayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData,
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(publishPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(publishPacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.AreEqual(publishPacket.Dup, deserialized.Dup);
        Assert.AreEqual(publishPacket.Retain, deserialized.Retain);
        CollectionAssert.AreEqual(publishPacket.Payload.ToArray(), deserialized.Payload.ToArray());
        Assert.AreEqual(publishPacket.QualityOfServiceLevel, deserialized.QualityOfServiceLevel);
        Assert.AreEqual(publishPacket.Topic, deserialized.Topic);
        Assert.IsNull(deserialized.ResponseTopic); // Not supported in v3.1.1.
        Assert.IsNull(deserialized.ContentType); // Not supported in v3.1.1.
        CollectionAssert.AreEqual(null, deserialized.CorrelationData); // Not supported in v3.1.1.
        Assert.AreEqual(0U, deserialized.TopicAlias); // Not supported in v3.1.1.
        CollectionAssert.AreEqual(null, deserialized.SubscriptionIdentifiers); // Not supported in v3.1.1
        Assert.AreEqual(0U, deserialized.MessageExpiryInterval); // Not supported in v3.1.1
        Assert.AreEqual(MqttPayloadFormatIndicator.Unspecified, deserialized.PayloadFormatIndicator); // Not supported in v3.1.1
        Assert.IsNull(deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttPubRecPacket_V311()
    {
        var pubRecPacket = new MqttPubRecPacket
        {
            PacketIdentifier = 123,
            ReasonCode = MqttPubRecReasonCode.UnspecifiedError,
            ReasonString = "ReasonString",
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubRecPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(pubRecPacket.PacketIdentifier, deserialized.PacketIdentifier);
        // ReasonCode not available in MQTTv3.
        // ReasonString not available in MQTTv3.
        // UserProperties not available in MQTTv3.
        Assert.IsNull(deserialized.UserProperties);
    }

    [TestMethod]
    public void Serialize_Full_MqttPubRelPacket_V311()
    {
        var pubRelPacket = new MqttPubRelPacket
        {
            PacketIdentifier = 123,
            ReasonCode = MqttPubRelReasonCode.PacketIdentifierNotFound,
            ReasonString = "ReasonString",
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubRelPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(pubRelPacket.PacketIdentifier, deserialized.PacketIdentifier);
        // ReasonCode not available in MQTTv3.
        // ReasonString not available in MQTTv3.
        // UserProperties not available in MQTTv3.
        Assert.IsNull(deserialized.UserProperties);
    }

    [TestMethod]
    public void Serialize_Full_MqttSubAckPacket_V311()
    {
        var subAckPacket = new MqttSubAckPacket
        {
            PacketIdentifier = 123,
            ReasonString = "ReasonString",
            ReasonCodes = [MqttSubscribeReasonCode.GrantedQoS1],
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(subAckPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(subAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        Assert.HasCount(subAckPacket.ReasonCodes.Count, deserialized.ReasonCodes);
        Assert.AreEqual(subAckPacket.ReasonCodes[0], deserialized.ReasonCodes[0]);
        CollectionAssert.AreEqual(null, deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttSubscribePacket_V311()
    {
        var subscribePacket = new MqttSubscribePacket
        {
            PacketIdentifier = 123,
            SubscriptionIdentifier = 456,
            TopicFilters =
            [
                new()
                {
                    Topic = "Topic",
                    NoLocal = true,
                    RetainHandling = MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly,
                    RetainAsPublished = true,
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                }
            ],
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(subscribePacket, MqttProtocolVersion.V311);

        Assert.AreEqual(subscribePacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.AreEqual(0U, deserialized.SubscriptionIdentifier); // Not supported in v3.1.1
        Assert.HasCount(1, deserialized.TopicFilters);
        Assert.AreEqual(subscribePacket.TopicFilters[0].Topic, deserialized.TopicFilters[0].Topic);
        Assert.IsFalse(deserialized.TopicFilters[0].NoLocal); // Not supported in v3.1.1
        Assert.AreEqual(MqttRetainHandling.SendAtSubscribe, deserialized.TopicFilters[0].RetainHandling); // Not supported in v3.1.1
        Assert.IsFalse(deserialized.TopicFilters[0].RetainAsPublished); // Not supported in v3.1.1
        Assert.AreEqual(subscribePacket.TopicFilters[0].QualityOfServiceLevel, deserialized.TopicFilters[0].QualityOfServiceLevel);
        CollectionAssert.AreEqual(null, deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttUnsubAckPacket_V311()
    {
        var unsubAckPacket = new MqttUnsubAckPacket
        {
            PacketIdentifier = 123,
            ReasonCodes = [MqttUnsubscribeReasonCode.ImplementationSpecificError],
            ReasonString = "ReasonString",
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(unsubAckPacket, MqttProtocolVersion.V311);

        Assert.AreEqual(unsubAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.IsNull(deserialized.ReasonString); // Not supported in v3.1.1
        CollectionAssert.AreEqual(null, deserialized.ReasonCodes); // Not supported in v3.1.1
        CollectionAssert.AreEqual(null, deserialized.UserProperties); // Not supported in v3.1.1
    }

    [TestMethod]
    public void Serialize_Full_MqttUnsubscribePacket_V311()
    {
        var unsubscribePacket = new MqttUnsubscribePacket
        {
            PacketIdentifier = 123,
            TopicFilters = ["TopicFilter1"],
            UserProperties = [new("Foo", Encoding.UTF8.GetBytes("Bar"))]
        };

        var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(unsubscribePacket, MqttProtocolVersion.V311);

        Assert.AreEqual(unsubscribePacket.PacketIdentifier, deserialized.PacketIdentifier);
        Assert.HasCount(unsubscribePacket.TopicFilters.Count, deserialized.TopicFilters);
        Assert.AreEqual(unsubscribePacket.TopicFilters[0], deserialized.TopicFilters[0]);
        CollectionAssert.AreEqual(null, deserialized.UserProperties);
    }
}