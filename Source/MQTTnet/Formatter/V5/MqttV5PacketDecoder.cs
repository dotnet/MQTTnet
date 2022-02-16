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
    public sealed class MqttV5PacketDecoder
    {
        readonly MqttBufferReader _bufferReader = new MqttBufferReader();

        public MqttPacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket.TotalLength == 0)
            {
                return null;
            }

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect:
                    return DecodeConnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck:
                    return DecodeConnAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Disconnect:
                    return DecodeDisconnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Publish:
                    return DecodePublishPacket(receivedMqttPacket.FixedHeader, receivedMqttPacket.Body);
                case MqttControlPacketType.PubAck:
                    return DecodePubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRec:
                    return DecodePubRecPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRel:
                    return DecodePubRelPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubComp:
                    return DecodePubCompPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PingReq:
                    return MqttPingReqPacket.Instance;
                case MqttControlPacketType.PingResp:
                    return MqttPingRespPacket.Instance;
                case MqttControlPacketType.Subscribe:
                    return DecodeSubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck:
                    return DecodeSubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscibe:
                    return DecodeUnsubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck:
                    return DecodeUnsubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Auth:
                    return DecodeAuthPacket(receivedMqttPacket.Body);

                default:
                    throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        MqttPacket DecodeAuthPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttAuthPacket();

            if (_bufferReader.EndOfStream)
            {
                packet.ReasonCode = MqttAuthenticateReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttAuthenticateReasonCode)_bufferReader.ReadByte();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

        MqttPacket DecodeConnAckPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var acknowledgeFlags = _bufferReader.ReadByte();

            var packet = new MqttConnAckPacket
            {
                IsSessionPresent = (acknowledgeFlags & 0x1) > 0,
                ReasonCode = (MqttConnectReasonCode)_bufferReader.ReadByte(),
                // indicate that a feature is available.
                // Set all default values according to specification. When they are missing the often
                RetainAvailable = true,
                SharedSubscriptionAvailable = true,
                SubscriptionIdentifiersAvailable = true,
                WildcardSubscriptionAvailable = true,
                // Absence indicates max QoS level.
                MaximumQoS = MqttQualityOfServiceLevel.ExactlyOnce
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

        MqttPacket DecodeConnectPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttConnectPacket
            {
                // If the Request Problem Information is absent, the value of 1 is used.
                RequestProblemInformation = true 
            };

            var protocolName = _bufferReader.ReadString();
            var protocolVersion = _bufferReader.ReadByte();

            if (protocolName != "MQTT" && protocolVersion != 5)
            {
                throw new MqttProtocolViolationException("MQTT protocol name and version do not match MQTT v5.");
            }

            var connectFlags = _bufferReader.ReadByte();

            var cleanSessionFlag = (connectFlags & 0x02) > 0;
            var willMessageFlag = (connectFlags & 0x04) > 0;
            var willMessageQoS = (byte)((connectFlags >> 3) & 3);
            var willMessageRetainFlag = (connectFlags & 0x20) > 0;
            var passwordFlag = (connectFlags & 0x40) > 0;
            var usernameFlag = (connectFlags & 0x80) > 0;

            packet.CleanSession = cleanSessionFlag;

            if (willMessageFlag)
            {
                packet.WillFlag = true;
                packet.WillQoS = (MqttQualityOfServiceLevel)willMessageQoS;
                packet.WillRetain = willMessageRetainFlag;
            }

            packet.KeepAlivePeriod = _bufferReader.ReadTwoByteInteger();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            packet.ClientId = _bufferReader.ReadString();

            if (packet.WillFlag)
            {
                var willPropertiesReader = new MqttV5PropertiesReader(_bufferReader);

                while (willPropertiesReader.MoveNext())
                {
                    if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.PayloadFormatIndicator)
                    {
                        packet.WillPayloadFormatIndicator = willPropertiesReader.ReadPayloadFormatIndicator();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.MessageExpiryInterval)
                    {
                        packet.WillMessageExpiryInterval = willPropertiesReader.ReadMessageExpiryInterval();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ResponseTopic)
                    {
                        packet.WillResponseTopic = willPropertiesReader.ReadResponseTopic();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.CorrelationData)
                    {
                        packet.WillCorrelationData = willPropertiesReader.ReadCorrelationData();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.ContentType)
                    {
                        packet.WillContentType = willPropertiesReader.ReadContentType();
                    }
                    else if (willPropertiesReader.CurrentPropertyId == MqttPropertyId.WillDelayInterval)
                    {
                        packet.WillDelayInterval = willPropertiesReader.ReadWillDelayInterval();
                    }
                    else
                    {
                        willPropertiesReader.ThrowInvalidPropertyIdException(typeof(MqttPublishPacket));
                    }
                }

                packet.WillTopic = _bufferReader.ReadString();
                packet.WillMessage = _bufferReader.ReadBinaryData();
                packet.WillUserProperties = willPropertiesReader.CollectedUserProperties;
            }

            if (usernameFlag)
            {
                packet.Username = _bufferReader.ReadString();
            }

            if (passwordFlag)
            {
                packet.Password = _bufferReader.ReadBinaryData();
            }

            return packet;
        }

        MqttPacket DecodeDisconnectPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttDisconnectPacket
            {
                ReasonCode = (MqttDisconnectReasonCode)_bufferReader.ReadByte()
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            return packet;
        }
        
        MqttPacket DecodePubAckPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttPubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            if (_bufferReader.EndOfStream)
            {
                packet.ReasonCode = MqttPubAckReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubAckReasonCode)_bufferReader.ReadByte();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

        MqttPacket DecodePubCompPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttPubCompPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            if (_bufferReader.EndOfStream)
            {
                packet.ReasonCode = MqttPubCompReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubCompReasonCode)_bufferReader.ReadByte();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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


        MqttPacket DecodePublishPacket(byte header, ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var retain = (header & 1) > 0;
            var qos = (MqttQualityOfServiceLevel)((header >> 1) & 3);
            var dup = ((header >> 3) & 1) > 0;

            var packet = new MqttPublishPacket
            {
                Topic = _bufferReader.ReadString(),
                Retain = retain,
                QualityOfServiceLevel = qos,
                Dup = dup
            };

            if (qos > 0)
            {
                packet.PacketIdentifier = _bufferReader.ReadTwoByteInteger();
            }

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            if (!_bufferReader.EndOfStream)
            {
                packet.Payload = _bufferReader.ReadRemainingData();
            }

            return packet;
        }

        MqttPacket DecodePubRecPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttPubRecPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            if (_bufferReader.EndOfStream)
            {
                packet.ReasonCode = MqttPubRecReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubRecReasonCode)_bufferReader.ReadByte();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

        MqttPacket DecodePubRelPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttPubRelPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            if (_bufferReader.EndOfStream)
            {
                packet.ReasonCode = MqttPubRelReasonCode.Success;
                return packet;
            }

            packet.ReasonCode = (MqttPubRelReasonCode)_bufferReader.ReadByte();

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

        MqttPacket DecodeSubAckPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            packet.UserProperties = propertiesReader.CollectedUserProperties;
            
            while (!_bufferReader.EndOfStream)
            {
                var reasonCode = (MqttSubscribeReasonCode)_bufferReader.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        MqttPacket DecodeSubscribePacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            while (!_bufferReader.EndOfStream)
            {
                var topic = _bufferReader.ReadString();
                var options = _bufferReader.ReadByte();

                var qos = (MqttQualityOfServiceLevel)(options & 3);
                var noLocal = (options & (1 << 2)) > 0;
                var retainAsPublished = (options & (1 << 3)) > 0;
                var retainHandling = (MqttRetainHandling)((options >> 4) & 3);

                packet.TopicFilters.Add(
                    new MqttTopicFilter
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

        MqttPacket DecodeUnsubAckPacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttUnsubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
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

            packet.ReasonCodes = new List<MqttUnsubscribeReasonCode>(_bufferReader.BytesLeft);
            
            while (!_bufferReader.EndOfStream)
            {
                var reasonCode = (MqttUnsubscribeReasonCode)_bufferReader.ReadByte();
                packet.ReasonCodes.Add(reasonCode);
            }

            return packet;
        }

        MqttPacket DecodeUnsubscribePacket(ArraySegment<byte> body)
        {
            ThrowIfBodyIsEmpty(body);

            _bufferReader.SetBuffer(body.Array, body.Offset, body.Count);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            var propertiesReader = new MqttV5PropertiesReader(_bufferReader);
            while (propertiesReader.MoveNext())
            {
                propertiesReader.ThrowInvalidPropertyIdException(typeof(MqttUnsubscribePacket));
            }

            packet.UserProperties = propertiesReader.CollectedUserProperties;

            while (!_bufferReader.EndOfStream)
            {
                packet.TopicFilters.Add(_bufferReader.ReadString());
            }

            return packet;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void ThrowIfBodyIsEmpty(ArraySegment<byte> body)
        {
            if (body.Count == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}