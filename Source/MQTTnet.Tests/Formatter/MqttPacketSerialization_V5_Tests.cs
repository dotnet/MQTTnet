// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttPacketSerialization_V5_Tests
    {
        [TestMethod]
        public void Serialize_Full_MqttAuthPacket_V500()
        {
            var authPacket = new MqttAuthPacket
            {
                AuthenticationData = Encoding.UTF8.GetBytes("AuthenticationData"),
                AuthenticationMethod = "AuthenticationMethod",
                ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication,
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(authPacket, MqttProtocolVersion.V500);

            CollectionAssert.AreEqual(authPacket.AuthenticationData, deserialized.AuthenticationData);
            Assert.AreEqual(authPacket.AuthenticationMethod, deserialized.AuthenticationMethod);
            Assert.AreEqual(authPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(authPacket.ReasonString, deserialized.ReasonString);
            CollectionAssert.AreEqual(authPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttConnAckPacket_V500()
        {
            var connAckPacket = new MqttConnAckPacket
            {
                AuthenticationData = Encoding.UTF8.GetBytes("AuthenticationData"),
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
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(connAckPacket, MqttProtocolVersion.V500);

            CollectionAssert.AreEqual(connAckPacket.AuthenticationData, deserialized.AuthenticationData);
            Assert.AreEqual(connAckPacket.AuthenticationMethod, deserialized.AuthenticationMethod);
            Assert.AreEqual(connAckPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(connAckPacket.ReasonString, deserialized.ReasonString);
            Assert.AreEqual(connAckPacket.ReceiveMaximum, deserialized.ReceiveMaximum);
            Assert.AreEqual(connAckPacket.ResponseInformation, deserialized.ResponseInformation);
            Assert.AreEqual(connAckPacket.RetainAvailable, deserialized.RetainAvailable);
            // Return Code only used in MQTTv3
            Assert.AreEqual(connAckPacket.ServerReference, deserialized.ServerReference);
            Assert.AreEqual(connAckPacket.AssignedClientIdentifier, deserialized.AssignedClientIdentifier);
            Assert.AreEqual(connAckPacket.IsSessionPresent, deserialized.IsSessionPresent);
            Assert.AreEqual(connAckPacket.MaximumPacketSize, deserialized.MaximumPacketSize);
            Assert.AreEqual(connAckPacket.MaximumQoS, deserialized.MaximumQoS);
            Assert.AreEqual(connAckPacket.ServerKeepAlive, deserialized.ServerKeepAlive);
            Assert.AreEqual(connAckPacket.SessionExpiryInterval, deserialized.SessionExpiryInterval);
            Assert.AreEqual(connAckPacket.SharedSubscriptionAvailable, deserialized.SharedSubscriptionAvailable);
            Assert.AreEqual(connAckPacket.SubscriptionIdentifiersAvailable, deserialized.SubscriptionIdentifiersAvailable);
            Assert.AreEqual(connAckPacket.TopicAliasMaximum, deserialized.TopicAliasMaximum);
            Assert.AreEqual(connAckPacket.WildcardSubscriptionAvailable, deserialized.WildcardSubscriptionAvailable);
            CollectionAssert.AreEqual(connAckPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttConnectPacket_V500()
        {
            var connectPacket = new MqttConnectPacket
            {
                Username = "Username",
                Password = Encoding.UTF8.GetBytes("Password"),
                ClientId = "ClientId",
                AuthenticationData = Encoding.UTF8.GetBytes("AuthenticationData"),
                AuthenticationMethod = "AuthenticationMethod",
                CleanSession = true,
                ReceiveMaximum = 123,
                WillFlag = true,
                WillTopic = "WillTopic",
                WillMessage = Encoding.UTF8.GetBytes("WillMessage"),
                WillRetain = true,
                KeepAlivePeriod = 456,
                MaximumPacketSize = 789,
                RequestProblemInformation = true,
                RequestResponseInformation = true,
                SessionExpiryInterval = 27,
                TopicAliasMaximum = 67,
                WillContentType = "WillContentType",
                WillCorrelationData = Encoding.UTF8.GetBytes("WillCorrelationData"),
                WillDelayInterval = 782,
                WillQoS = MqttQualityOfServiceLevel.ExactlyOnce,
                WillResponseTopic = "WillResponseTopic",
                WillMessageExpiryInterval = 542,
                WillPayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData,
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                },
                WillUserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("WillFoo", "WillBar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(connectPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(connectPacket.Username, deserialized.Username);
            CollectionAssert.AreEqual(connectPacket.Password, deserialized.Password);
            Assert.AreEqual(connectPacket.ClientId, deserialized.ClientId);
            CollectionAssert.AreEqual(connectPacket.AuthenticationData, deserialized.AuthenticationData);
            Assert.AreEqual(connectPacket.AuthenticationMethod, deserialized.AuthenticationMethod);
            Assert.AreEqual(connectPacket.CleanSession, deserialized.CleanSession);
            Assert.AreEqual(connectPacket.ReceiveMaximum, deserialized.ReceiveMaximum);
            Assert.AreEqual(connectPacket.WillFlag, deserialized.WillFlag);
            Assert.AreEqual(connectPacket.WillTopic, deserialized.WillTopic);
            CollectionAssert.AreEqual(connectPacket.WillMessage, deserialized.WillMessage);
            Assert.AreEqual(connectPacket.WillRetain, deserialized.WillRetain);
            Assert.AreEqual(connectPacket.KeepAlivePeriod, deserialized.KeepAlivePeriod);
            Assert.AreEqual(connectPacket.MaximumPacketSize, deserialized.MaximumPacketSize);
            Assert.AreEqual(connectPacket.RequestProblemInformation, deserialized.RequestProblemInformation);
            Assert.AreEqual(connectPacket.RequestResponseInformation, deserialized.RequestResponseInformation);
            Assert.AreEqual(connectPacket.SessionExpiryInterval, deserialized.SessionExpiryInterval);
            Assert.AreEqual(connectPacket.TopicAliasMaximum, deserialized.TopicAliasMaximum);
            Assert.AreEqual(connectPacket.WillContentType, deserialized.WillContentType);
            CollectionAssert.AreEqual(connectPacket.WillCorrelationData, deserialized.WillCorrelationData);
            Assert.AreEqual(connectPacket.WillDelayInterval, deserialized.WillDelayInterval);
            Assert.AreEqual(connectPacket.WillQoS, deserialized.WillQoS);
            Assert.AreEqual(connectPacket.WillResponseTopic, deserialized.WillResponseTopic);
            Assert.AreEqual(connectPacket.WillMessageExpiryInterval, deserialized.WillMessageExpiryInterval);
            Assert.AreEqual(connectPacket.WillPayloadFormatIndicator, deserialized.WillPayloadFormatIndicator);
            CollectionAssert.AreEqual(connectPacket.UserProperties, deserialized.UserProperties);
            CollectionAssert.AreEqual(connectPacket.WillUserProperties, deserialized.WillUserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttDisconnectPacket_V500()
        {
            var disconnectPacket = new MqttDisconnectPacket
            {
                ReasonCode = MqttDisconnectReasonCode.QuotaExceeded,
                ReasonString = "ReasonString",
                ServerReference = "ServerReference",
                SessionExpiryInterval = 234,
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(disconnectPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(disconnectPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(disconnectPacket.ReasonString, deserialized.ReasonString);
            Assert.AreEqual(disconnectPacket.ServerReference, deserialized.ServerReference);
            Assert.AreEqual(disconnectPacket.SessionExpiryInterval, deserialized.SessionExpiryInterval);
            CollectionAssert.AreEqual(disconnectPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttPingReqPacket_V500()
        {
            var pingReqPacket = new MqttPingReqPacket();

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pingReqPacket, MqttProtocolVersion.V500);

            Assert.IsNotNull(deserialized);
        }

        [TestMethod]
        public void Serialize_Full_MqttPingRespPacket_V500()
        {
            var pingRespPacket = new MqttPingRespPacket();

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pingRespPacket, MqttProtocolVersion.V500);

            Assert.IsNotNull(deserialized);
        }

        [TestMethod]
        public void Serialize_Full_MqttPubAckPacket_V500()
        {
            var pubAckPacket = new MqttPubAckPacket
            {
                PacketIdentifier = 123,
                ReasonCode = MqttPubAckReasonCode.NoMatchingSubscribers,
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubAckPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(pubAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(pubAckPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(pubAckPacket.ReasonString, deserialized.ReasonString);
            CollectionAssert.AreEqual(pubAckPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttPubCompPacket_V500()
        {
            var pubCompPacket = new MqttPubCompPacket
            {
                PacketIdentifier = 123,
                ReasonCode = MqttPubCompReasonCode.PacketIdentifierNotFound,
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubCompPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(pubCompPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(pubCompPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(pubCompPacket.ReasonString, deserialized.ReasonString);
            CollectionAssert.AreEqual(pubCompPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttPublishPacket_V500()
        {
            var publishPacket = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                PayloadSequence = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Payload")),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "Topic",
                ResponseTopic = "/Response",
                ContentType = "Content-Type",
                CorrelationData = Encoding.UTF8.GetBytes("CorrelationData"),
                TopicAlias = 27,
                SubscriptionIdentifiers = new List<uint>
                {
                    123
                },
                MessageExpiryInterval = 38,
                PayloadFormatIndicator = MqttPayloadFormatIndicator.CharacterData,
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(publishPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(publishPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(publishPacket.Dup, deserialized.Dup);
            Assert.AreEqual(publishPacket.Retain, deserialized.Retain);
            CollectionAssert.AreEqual(publishPacket.PayloadSequence.ToArray(), deserialized.PayloadSequence.ToArray());
            Assert.AreEqual(publishPacket.QualityOfServiceLevel, deserialized.QualityOfServiceLevel);
            Assert.AreEqual(publishPacket.Topic, deserialized.Topic);
            Assert.AreEqual(publishPacket.ResponseTopic, deserialized.ResponseTopic);
            Assert.AreEqual(publishPacket.ContentType, deserialized.ContentType);
            CollectionAssert.AreEqual(publishPacket.CorrelationData, deserialized.CorrelationData);
            Assert.AreEqual(publishPacket.TopicAlias, deserialized.TopicAlias);
            CollectionAssert.AreEqual(publishPacket.SubscriptionIdentifiers, deserialized.SubscriptionIdentifiers);
            Assert.AreEqual(publishPacket.MessageExpiryInterval, deserialized.MessageExpiryInterval);
            Assert.AreEqual(publishPacket.PayloadFormatIndicator, deserialized.PayloadFormatIndicator);
            CollectionAssert.AreEqual(publishPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttPubRecPacket_V500()
        {
            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = 123,
                ReasonCode = MqttPubRecReasonCode.UnspecifiedError,
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubRecPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(pubRecPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(pubRecPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(pubRecPacket.ReasonString, deserialized.ReasonString);
            CollectionAssert.AreEqual(pubRecPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttPubRelPacket_V500()
        {
            var pubRelPacket = new MqttPubRelPacket
            {
                PacketIdentifier = 123,
                ReasonCode = MqttPubRelReasonCode.PacketIdentifierNotFound,
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(pubRelPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(pubRelPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(pubRelPacket.ReasonCode, deserialized.ReasonCode);
            Assert.AreEqual(pubRelPacket.ReasonString, deserialized.ReasonString);
            CollectionAssert.AreEqual(pubRelPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttSubAckPacket_V500()
        {
            var subAckPacket = new MqttSubAckPacket
            {
                PacketIdentifier = 123,
                ReasonString = "ReasonString",
                ReasonCodes = new List<MqttSubscribeReasonCode>
                {
                    MqttSubscribeReasonCode.GrantedQoS1
                },
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(subAckPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(subAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(subAckPacket.ReasonString, deserialized.ReasonString);
            Assert.AreEqual(subAckPacket.ReasonCodes.Count, deserialized.ReasonCodes.Count);
            Assert.AreEqual(subAckPacket.ReasonCodes[0], deserialized.ReasonCodes[0]);
            CollectionAssert.AreEqual(subAckPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttSubscribePacket_V500()
        {
            var subscribePacket = new MqttSubscribePacket
            {
                PacketIdentifier = 123,
                SubscriptionIdentifier = 456,
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilter
                    {
                        Topic = "Topic",
                        NoLocal = true,
                        RetainHandling = MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly,
                        RetainAsPublished = true,
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
                    }
                },
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(subscribePacket, MqttProtocolVersion.V500);

            Assert.AreEqual(subscribePacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(subscribePacket.SubscriptionIdentifier, deserialized.SubscriptionIdentifier);
            Assert.AreEqual(1, deserialized.TopicFilters.Count);
            Assert.AreEqual(subscribePacket.TopicFilters[0].Topic, deserialized.TopicFilters[0].Topic);
            Assert.AreEqual(subscribePacket.TopicFilters[0].NoLocal, deserialized.TopicFilters[0].NoLocal);
            Assert.AreEqual(subscribePacket.TopicFilters[0].RetainHandling, deserialized.TopicFilters[0].RetainHandling);
            Assert.AreEqual(subscribePacket.TopicFilters[0].RetainAsPublished, deserialized.TopicFilters[0].RetainAsPublished);
            Assert.AreEqual(subscribePacket.TopicFilters[0].QualityOfServiceLevel, deserialized.TopicFilters[0].QualityOfServiceLevel);
            CollectionAssert.AreEqual(subscribePacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttUnsubAckPacket_V500()
        {
            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = 123,
                ReasonCodes = new List<MqttUnsubscribeReasonCode>
                {
                    MqttUnsubscribeReasonCode.ImplementationSpecificError
                },
                ReasonString = "ReasonString",
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(unsubAckPacket, MqttProtocolVersion.V500);

            Assert.AreEqual(unsubAckPacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(unsubAckPacket.ReasonString, deserialized.ReasonString);
            Assert.AreEqual(unsubAckPacket.ReasonCodes.Count, deserialized.ReasonCodes.Count);
            Assert.AreEqual(unsubAckPacket.ReasonCodes[0], deserialized.ReasonCodes[0]);
            CollectionAssert.AreEqual(unsubAckPacket.UserProperties, deserialized.UserProperties);
        }

        [TestMethod]
        public void Serialize_Full_MqttUnsubscribePacket_V500()
        {
            var unsubscribePacket = new MqttUnsubscribePacket
            {
                PacketIdentifier = 123,
                TopicFilters = new List<string>
                {
                    "TopicFilter1"
                },
                UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("Foo", "Bar")
                }
            };

            var deserialized = MqttPacketSerializationHelper.EncodeAndDecodePacket(unsubscribePacket, MqttProtocolVersion.V500);

            Assert.AreEqual(unsubscribePacket.PacketIdentifier, deserialized.PacketIdentifier);
            Assert.AreEqual(unsubscribePacket.TopicFilters.Count, deserialized.TopicFilters.Count);
            Assert.AreEqual(unsubscribePacket.TopicFilters[0], deserialized.TopicFilters[0]);
            CollectionAssert.AreEqual(unsubscribePacket.UserProperties, deserialized.UserProperties);
        }
    }
}