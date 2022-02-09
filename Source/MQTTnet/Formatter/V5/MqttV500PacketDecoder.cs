// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V5
{
    public class MqttV500PacketDecoder
    {
        static readonly MqttPingReqPacket PingReqPacket = new MqttPingReqPacket();

        static readonly MqttPingRespPacket PingRespPacket = new MqttPingRespPacket();

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect: return DecodeConnectPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.ConnAck: return DecodeConnAckPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.Disconnect: return DecodeDisconnectPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.Publish: return DecodePublishPacket(receivedMqttPacket.FixedHeader, receivedMqttPacket.BodyReader);
                case MqttControlPacketType.PubAck: return DecodePubAckPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.PubRec: return DecodePubRecPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.PubRel: return DecodePubRelPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.PubComp: return DecodePubCompPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.PingReq: return DecodePingReqPacket();
                case MqttControlPacketType.PingResp: return DecodePingRespPacket();
                case MqttControlPacketType.Subscribe: return DecodeSubscribePacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.SubAck: return DecodeSubAckPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.Unsubscibe: return DecodeUnsubscribePacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.UnsubAck: return DecodeUnsubAckPacket(receivedMqttPacket.BodyReader);
                case MqttControlPacketType.Auth: return DecodeAuthPacket(receivedMqttPacket.BodyReader);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        static MqttBasePacket DecodeConnectPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnectPacket();

            var protocolName = body.ReadStringWithLengthPrefix();
            var protocolVersion = body.ReadByte();

            if (protocolName != "MQTT" && protocolVersion != 5)
            {
                throw new MqttProtocolViolationException("MQTT protocol name and version do not match MQTT v5.");
            }

            var connectFlags = body.ReadByte();

            var cleanSessionFlag = (connectFlags & 0x02) > 0;
            var willMessageFlag = (connectFlags & 0x04) > 0;
            var willMessageQoS = (byte)(connectFlags >> 3 & 3);
            var willMessageRetainFlag = (connectFlags & 0x20) > 0;
            var passwordFlag = (connectFlags & 0x40) > 0;
            var usernameFlag = (connectFlags & 0x80) > 0;

            packet.CleanSession = cleanSessionFlag;

            if (willMessageFlag)
            {
                packet.WillFlag = true;
                packet.WillQoS = (MqttQualityOfServiceLevel) willMessageQoS;
                packet.WillRetain = willMessageRetainFlag;
            }

            packet.KeepAlivePeriod = body.ReadTwoByteInteger();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReceiveMaximum)
                {
                    packet.ReceiveMaximum = propertiesReader.ReadReceiveMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAliasMaximum)
                {
                    packet.TopicAliasMaximum = propertiesReader.ReadTopicAliasMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumPacketSize)
                {
                    packet.MaximumPacketSize = propertiesReader.ReadMaximumPacketSize();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RequestResponseInformation)
                {
                    packet.RequestResponseInformation = propertiesReader.RequestResponseInformation();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RequestProblemInformation)
                {
                    packet.RequestProblemInformation = propertiesReader.RequestProblemInformation();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttConnectPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            packet.ClientId = body.ReadStringWithLengthPrefix();

            if (packet.WillFlag)
            {
                var willPropertiesReader = new MqttV500PropertiesReader(body);

                while (willPropertiesReader.MoveNext())
                {
                    if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.PayloadFormatIndicator)
                    {
                        packet.WillPayloadFormatIndicator = propertiesReader.ReadPayloadFormatIndicator();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.MessageExpiryInterval)
                    {
                        packet.WillMessageExpiryInterval = propertiesReader.ReadMessageExpiryInterval();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ResponseTopic)
                    {
                        packet.WillResponseTopic = propertiesReader.ReadResponseTopic();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.CorrelationData)
                    {
                        packet.WillCorrelationData = propertiesReader.ReadCorrelationData();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ContentType)
                    {
                        packet.WillContentType = propertiesReader.ReadContentType();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.WillDelayInterval)
                    {
                        packet.WillDelayInterval = propertiesReader.ReadWillDelayInterval();
                    }
                    else
                    {
                        propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPublishPacket));
                    }
                }

                packet.WillTopic = body.ReadStringWithLengthPrefix();
                packet.WillMessage = body.ReadWithLengthPrefix();
                packet.WillUserProperties = propertiesReader.CollectedUserProperties;
            }

            if (usernameFlag)
            {
                packet.Username = body.ReadStringWithLengthPrefix();
            }

            if (passwordFlag)
            {
                packet.Password = body.ReadWithLengthPrefix();
            }

            return packet;
        }

        static MqttBasePacket DecodeConnAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var acknowledgeFlags = body.ReadByte();

            var packet = new MqttConnAckPacket
            {
                IsSessionPresent = (acknowledgeFlags & 0x1) > 0,
                ReasonCode = (MqttConnectReasonCode)body.ReadByte(),
                // indicate that a feature is available.
                // Set all default values according to specification. When they are missing the often
                RetainAvailable = true,
                SharedSubscriptionAvailable = true,
                SubscriptionIdentifiersAvailable = true,
                WildcardSubscriptionAvailable = true
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RetainAvailable)
                {
                    packet.RetainAvailable = propertiesReader.ReadRetainAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReceiveMaximum)
                {
                    packet.ReceiveMaximum = propertiesReader.ReadReceiveMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumQoS)
                {
                    packet.MaximumQoS = propertiesReader.ReadMaximumQoS();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AssignedClientIdentifier)
                {
                    packet.AssignedClientIdentifier = propertiesReader.ReadAssignedClientIdentifier();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAliasMaximum)
                {
                    packet.TopicAliasMaximum = propertiesReader.ReadTopicAliasMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumPacketSize)
                {
                    packet.MaximumPacketSize = propertiesReader.ReadMaximumPacketSize();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.WildcardSubscriptionAvailable)
                {
                    packet.WildcardSubscriptionAvailable = propertiesReader.ReadWildcardSubscriptionAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifiersAvailable)
                {
                    packet.SubscriptionIdentifiersAvailable = propertiesReader.ReadSubscriptionIdentifiersAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SharedSubscriptionAvailable)
                {
                    packet.SharedSubscriptionAvailable = propertiesReader.ReadSharedSubscriptionAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerKeepAlive)
                {
                    packet.ServerKeepAlive = propertiesReader.ReadServerKeepAlive();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ResponseInformation)
                {
                    packet.ResponseInformation = propertiesReader.ReadResponseInformation();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerReference)
                {
                    packet.ServerReference = propertiesReader.ReadServerReference();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttConnAckPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;
            
            return packet;
        }

        static MqttBasePacket DecodeDisconnectPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttDisconnectPacket
            {
                ReasonCode = (MqttDisconnectReasonCode)body.ReadByte()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerReference)
                {
                    packet.ServerReference = propertiesReader.ReadServerReference();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttDisconnectPacket));
                }
            }

            packet.UserProperties = packet.UserProperties;
            
            return packet;
        }

        static MqttBasePacket DecodeSubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifier)
                {
                    packet.SubscriptionIdentifier = propertiesReader.ReadSubscriptionIdentifier();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttSubscribePacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            while (!body.EndOfStream)
            {
                var topic = body.ReadStringWithLengthPrefix();
                var options = body.ReadByte();

                var qos = (MqttQualityOfServiceLevel)(options & 3);
                var noLocal = (options & (1 << 2)) > 0;
                var retainAsPublished = (options & (1 << 3)) > 0;
                var retainHandling = (MqttRetainHandling)((options >> 4) & 3);

                packet.TopicFilters.Add(new MqttTopicFilter
                {
                    Topic = topic,
                    QualityOfServiceLevel = qos,
                    NoLocal = noLocal,
                    RetainAsPublished = retainAsPublished,
                    RetainHandling = retainHandling
                });
            }

            return packet;
        }

        static MqttBasePacket DecodeSubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttSubAckPacket));
                }
            }

            packet.UserProperties = packet.UserProperties;
            
            while (!body.EndOfStream)
            {
                var reasonCode = (MqttSubscribeReasonCode)body.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        static MqttBasePacket DecodeUnsubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttUnsubscribePacket));
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            while (!body.EndOfStream)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        static MqttBasePacket DecodeUnsubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttUnsubAckPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            while (!body.EndOfStream)
            {
                var reasonCode = (MqttUnsubscribeReasonCode)body.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        static MqttBasePacket DecodePingReqPacket()
        {
            return PingReqPacket;
        }

        static MqttBasePacket DecodePingRespPacket()
        {
            return PingRespPacket;
        }

        static MqttBasePacket DecodePublishPacket(byte header, IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var retain = (header & 1) > 0;
            var qos = (MqttQualityOfServiceLevel)(header >> 1 & 3);
            var dup = (header >> 3 & 1) > 0;

            var packet = new MqttPublishPacket
            {
                Topic = body.ReadStringWithLengthPrefix(),
                Retain = retain,
                QualityOfServiceLevel = qos,
                Dup = dup
            };

            if (qos > 0)
            {
                packet.PacketIdentifier = body.ReadTwoByteInteger();
            }

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.PayloadFormatIndicator)
                {
                    packet.PayloadFormatIndicator = propertiesReader.ReadPayloadFormatIndicator();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MessageExpiryInterval)
                {
                    packet.MessageExpiryInterval = propertiesReader.ReadMessageExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAlias)
                {
                    packet.TopicAlias = propertiesReader.ReadTopicAlias();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ResponseTopic)
                {
                    packet.ResponseTopic = propertiesReader.ReadResponseTopic();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.CorrelationData)
                {
                    packet.CorrelationData = propertiesReader.ReadCorrelationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifier)
                {
                    if (packet.SubscriptionIdentifiers == null)
                    {
                        packet.SubscriptionIdentifiers = new List<uint>();
                    }
                    
                    packet.SubscriptionIdentifiers.Add(propertiesReader.ReadSubscriptionIdentifier());
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ContentType)
                {
                    packet.ContentType = propertiesReader.ReadContentType();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPublishPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            if (!body.EndOfStream)
            {
                packet.Payload = body.ReadRemainingData();
            }

            return packet;
        }

        static MqttBasePacket DecodePubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            if (body.EndOfStream)
            {
                packet.ReasonCode = MqttPubAckReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubAckReasonCode)body.ReadByte();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubAckPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;
            
            return packet;
        }

        static MqttBasePacket DecodePubRecPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            if (body.EndOfStream)
            {
                packet.ReasonCode = MqttPubRecReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubRecReasonCode)body.ReadByte();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubRecPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            return packet;
        }

        static MqttBasePacket DecodePubRelPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            if (body.EndOfStream)
            {
                packet.ReasonCode = MqttPubRelReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubRelReasonCode)body.ReadByte();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubRelPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;
            
            return packet;
        }

        static MqttBasePacket DecodePubCompPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            if (body.EndOfStream)
            {
                packet.ReasonCode = MqttPubCompReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubCompReasonCode)body.ReadByte();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubCompPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;
            
            return packet;
        }

        static MqttBasePacket DecodeAuthPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttAuthPacket();

            if (body.EndOfStream)
            {
                packet.ReasonCode = MqttAuthenticateReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttAuthenticateReasonCode)body.ReadByte();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.ReasonString = propertiesReader.ReadReasonString();
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttAuthPacket));
                }
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            return packet;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void ThrowIfBodyIsEmpty(IMqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
