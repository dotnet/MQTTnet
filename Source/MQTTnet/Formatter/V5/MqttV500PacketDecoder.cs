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
        private static readonly MqttPingReqPacket PingReqPacket = new MqttPingReqPacket();

        private static readonly MqttPingRespPacket PingRespPacket = new MqttPingRespPacket();

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1 || controlPacketType > 15)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect: return DecodeConnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck: return DecodeConnAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Disconnect: return DecodeDisconnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Publish: return DecodePublishPacket(receivedMqttPacket.FixedHeader, receivedMqttPacket.Body);
                case MqttControlPacketType.PubAck: return DecodePubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRec: return DecodePubRecPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRel: return DecodePubRelPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubComp: return DecodePubCompPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PingReq: return DecodePingReqPacket();
                case MqttControlPacketType.PingResp: return DecodePingRespPacket();
                case MqttControlPacketType.Subscribe: return DecodeSubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck: return DecodeSubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscibe: return DecodeUnsubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck: return DecodeUnsubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Auth: return DecodeAuthPacket(receivedMqttPacket.Body);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        private static MqttBasePacket DecodeConnectPacket(IMqttPacketBodyReader body)
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
                packet.WillMessage = new MqttApplicationMessage
                {
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)willMessageQoS,
                    Retain = willMessageRetainFlag
                };
            }

            packet.KeepAlivePeriod = body.ReadTwoByteInteger();

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttConnectPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.Properties.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.Properties.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.Properties.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReceiveMaximum)
                {
                    packet.Properties.ReceiveMaximum = propertiesReader.ReadReceiveMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAliasMaximum)
                {
                    packet.Properties.TopicAliasMaximum = propertiesReader.ReadTopicAliasMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumPacketSize)
                {
                    packet.Properties.MaximumPacketSize = propertiesReader.ReadMaximumPacketSize();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RequestResponseInformation)
                {
                    packet.Properties.RequestResponseInformation = propertiesReader.RequestResponseInformation();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RequestProblemInformation)
                {
                    packet.Properties.RequestProblemInformation = propertiesReader.RequestProblemInformation();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttConnectPacket));
                }
            }

            packet.ClientId = body.ReadStringWithLengthPrefix();

            if (packet.WillMessage != null)
            {
                var willPropertiesReader = new MqttV500PropertiesReader(body);

                while (willPropertiesReader.MoveNext())
                {
                    if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.PayloadFormatIndicator)
                    {
                        packet.WillMessage.PayloadFormatIndicator = propertiesReader.ReadPayloadFormatIndicator();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.MessageExpiryInterval)
                    {
                        packet.WillMessage.MessageExpiryInterval = propertiesReader.ReadMessageExpiryInterval();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.TopicAlias)
                    {
                        packet.WillMessage.TopicAlias = propertiesReader.ReadTopicAlias();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ResponseTopic)
                    {
                        packet.WillMessage.ResponseTopic = propertiesReader.ReadResponseTopic();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.CorrelationData)
                    {
                        packet.WillMessage.CorrelationData = propertiesReader.ReadCorrelationData();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifier)
                    {
                        if (packet.WillMessage.SubscriptionIdentifiers == null)
                        {
                            packet.WillMessage.SubscriptionIdentifiers = new List<uint>();
                        }

                        packet.WillMessage.SubscriptionIdentifiers.Add(propertiesReader.ReadSubscriptionIdentifier());
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ContentType)
                    {
                        packet.WillMessage.ContentType = propertiesReader.ReadContentType();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.WillDelayInterval)
                    {
                        // This is a special case!
                        packet.Properties.WillDelayInterval = propertiesReader.ReadWillDelayInterval();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                    {
                        if (packet.WillMessage.UserProperties == null)
                        {
                            packet.WillMessage.UserProperties = new List<MqttUserProperty>();
                        }

                        propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                    }
                    else
                    {
                        propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPublishPacket));
                    }
                }

                packet.WillMessage.Topic = body.ReadStringWithLengthPrefix();
                packet.WillMessage.Payload = body.ReadWithLengthPrefix();
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

        private static MqttBasePacket DecodeConnAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var acknowledgeFlags = body.ReadByte();

            var packet = new MqttConnAckPacket
            {
                IsSessionPresent = (acknowledgeFlags & 0x1) > 0,
                ReasonCode = (MqttConnectReasonCode)body.ReadByte()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttConnAckPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.Properties.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.Properties.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.Properties.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.RetainAvailable)
                {
                    packet.Properties.RetainAvailable = propertiesReader.ReadRetainAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReceiveMaximum)
                {
                    packet.Properties.ReceiveMaximum = propertiesReader.ReadReceiveMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumQoS)
                {
                    packet.Properties.MaximumQoS = propertiesReader.ReadMaximumQoS();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AssignedClientIdentifier)
                {
                    packet.Properties.AssignedClientIdentifier = propertiesReader.ReadAssignedClientIdentifier();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAliasMaximum)
                {
                    packet.Properties.TopicAliasMaximum = propertiesReader.ReadTopicAliasMaximum();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MaximumPacketSize)
                {
                    packet.Properties.MaximumPacketSize = propertiesReader.ReadMaximumPacketSize();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.WildcardSubscriptionAvailable)
                {
                    packet.Properties.WildcardSubscriptionAvailable = propertiesReader.ReadWildcardSubscriptionAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifiersAvailable)
                {
                    packet.Properties.SubscriptionIdentifiersAvailable = propertiesReader.ReadSubscriptionIdentifiersAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SharedSubscriptionAvailable)
                {
                    packet.Properties.SharedSubscriptionAvailable = propertiesReader.ReadSharedSubscriptionAvailable();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerKeepAlive)
                {
                    packet.Properties.ServerKeepAlive = propertiesReader.ReadServerKeepAlive();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ResponseInformation)
                {
                    packet.Properties.ResponseInformation = propertiesReader.ReadResponseInformation();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerReference)
                {
                    packet.Properties.ServerReference = propertiesReader.ReadServerReference();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttConnAckPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodeDisconnectPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttDisconnectPacket
            {
                ReasonCode = (MqttDisconnectReasonCode)body.ReadByte()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttDisconnectPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SessionExpiryInterval)
                {
                    packet.Properties.SessionExpiryInterval = propertiesReader.ReadSessionExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ServerReference)
                {
                    packet.Properties.ServerReference = propertiesReader.ReadServerReference();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttDisconnectPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodeSubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttSubscribePacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifier)
                {
                    packet.Properties.SubscriptionIdentifier = propertiesReader.ReadSubscriptionIdentifier();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttSubscribePacket));
                }
            }

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

        private static MqttBasePacket DecodeSubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttSubAckPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttSubAckPacket));
                }
            }

            while (!body.EndOfStream)
            {
                var reasonCode = (MqttSubscribeReasonCode)body.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        private static MqttBasePacket DecodeUnsubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttUnsubscribePacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttUnsubscribePacket));
                }
            }

            while (!body.EndOfStream)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DecodeUnsubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV500PropertiesReader(body);
            while (propertiesReader.MoveNext())
            {
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttUnsubAckPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttUnsubAckPacket));
                }
            }

            while (!body.EndOfStream)
            {
                var reasonCode = (MqttUnsubscribeReasonCode)body.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        private static MqttBasePacket DecodePingReqPacket()
        {
            return PingReqPacket;
        }

        private static MqttBasePacket DecodePingRespPacket()
        {
            return PingRespPacket;
        }

        private static MqttBasePacket DecodePublishPacket(byte header, IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttPublishPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.PayloadFormatIndicator)
                {
                    packet.Properties.PayloadFormatIndicator = propertiesReader.ReadPayloadFormatIndicator();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.MessageExpiryInterval)
                {
                    packet.Properties.MessageExpiryInterval = propertiesReader.ReadMessageExpiryInterval();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.TopicAlias)
                {
                    packet.Properties.TopicAlias = propertiesReader.ReadTopicAlias();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ResponseTopic)
                {
                    packet.Properties.ResponseTopic = propertiesReader.ReadResponseTopic();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.CorrelationData)
                {
                    packet.Properties.CorrelationData = propertiesReader.ReadCorrelationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.SubscriptionIdentifier)
                {
                    if (packet.Properties.SubscriptionIdentifiers == null)
                    {
                        packet.Properties.SubscriptionIdentifiers = new List<uint>();
                    }

                    packet.Properties.SubscriptionIdentifiers.Add(propertiesReader.ReadSubscriptionIdentifier());
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ContentType)
                {
                    packet.Properties.ContentType = propertiesReader.ReadContentType();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPublishPacket));
                }
            }

            if (!body.EndOfStream)
            {
                packet.Payload = body.ReadRemainingData();
            }

            return packet;
        }

        private static MqttBasePacket DecodePubAckPacket(IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttPubAckPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubAckPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodePubRecPacket(IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttPubRecPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubRecPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodePubRelPacket(IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttPubRelPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubRelPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodePubCompPacket(IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttPubCompPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPubCompPacket));
                }
            }

            return packet;
        }

        private static MqttBasePacket DecodeAuthPacket(IMqttPacketBodyReader body)
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
                if (packet.Properties == null)
                {
                    packet.Properties = new MqttAuthPacketProperties();
                }

                if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationMethod)
                {
                    packet.Properties.AuthenticationMethod = propertiesReader.ReadAuthenticationMethod();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.AuthenticationData)
                {
                    packet.Properties.AuthenticationData = propertiesReader.ReadAuthenticationData();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.ReasonString)
                {
                    packet.Properties.ReasonString = propertiesReader.ReadReasonString();
                }
                else if (propertiesReader.CurrentPropertyId == MqttPropertyId.UserProperty)
                {
                    if (packet.Properties.UserProperties == null)
                    {
                        packet.Properties.UserProperties = new List<MqttUserProperty>();
                    }

                    propertiesReader.AddUserPropertyTo(packet.Properties.UserProperties);
                }
                else
                {
                    propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttAuthPacket));
                }
            }

            return packet;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowIfBodyIsEmpty(IMqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
