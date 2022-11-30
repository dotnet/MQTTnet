// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V5
{
    public sealed class MqttV5PacketEncoder
    {
        const int FixedHeaderSize = 1;
        
        readonly MqttBufferWriter _bufferWriter;
        readonly MqttV5PropertiesWriter _propertiesWriter = new MqttV5PropertiesWriter(new MqttBufferWriter(1024, 4096));

        public MqttV5PacketEncoder(MqttBufferWriter bufferWriter)
        {
            _bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
        }

        public MqttPacketBuffer Encode(MqttPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _bufferWriter.Reset(5);
            _bufferWriter.Seek(5);

            var fixedHeader = EncodePacket(packet);
            var remainingLength = (uint)_bufferWriter.Length - 5;

            var publishPacket = packet as MqttPublishPacket;
            if (publishPacket?.Payload != null)
            {
                remainingLength += (uint)publishPacket.Payload.Length;
            }

            var remainingLengthSize = MqttBufferWriter.GetVariableByteIntegerSize(remainingLength);

            var headerSize = FixedHeaderSize + remainingLengthSize;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginning of array (has leading 0x0)
            _bufferWriter.Seek(headerOffset);
            _bufferWriter.WriteByte(fixedHeader);
            _bufferWriter.WriteVariableByteInteger(remainingLength);

            var buffer = _bufferWriter.GetBuffer();

            var firstSegment = new ArraySegment<byte>(buffer, headerOffset, _bufferWriter.Length - headerOffset);

            if (publishPacket?.Payload != null)
            {
                var payloadSegment = new ArraySegment<byte>(publishPacket.Payload, 0, publishPacket.Payload.Length);
                return new MqttPacketBuffer(firstSegment, payloadSegment);
            }

            return new MqttPacketBuffer(firstSegment);
        }
        
        byte EncodeAuthPacket(MqttAuthPacket packet)
        {
            _bufferWriter.WriteByte((byte)packet.ReasonCode);

            _propertiesWriter.WriteAuthenticationMethod(packet.AuthenticationMethod);
            _propertiesWriter.WriteAuthenticationData(packet.AuthenticationData);
            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Auth);
        }

        byte EncodeConnAckPacket(MqttConnAckPacket packet)
        {
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            _bufferWriter.WriteByte(connectAcknowledgeFlags);
            _bufferWriter.WriteByte((byte)packet.ReasonCode);

            _propertiesWriter.WriteSessionExpiryInterval(packet.SessionExpiryInterval);
            _propertiesWriter.WriteAuthenticationMethod(packet.AuthenticationMethod);
            _propertiesWriter.WriteAuthenticationData(packet.AuthenticationData);
            _propertiesWriter.WriteRetainAvailable(packet.RetainAvailable);
            _propertiesWriter.WriteReceiveMaximum(packet.ReceiveMaximum);
            _propertiesWriter.WriteMaximumQoS(packet.MaximumQoS);
            _propertiesWriter.WriteAssignedClientIdentifier(packet.AssignedClientIdentifier);
            _propertiesWriter.WriteTopicAliasMaximum(packet.TopicAliasMaximum);
            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteMaximumPacketSize(packet.MaximumPacketSize);
            _propertiesWriter.WriteWildcardSubscriptionAvailable(packet.WildcardSubscriptionAvailable);
            _propertiesWriter.WriteSubscriptionIdentifiersAvailable(packet.SubscriptionIdentifiersAvailable);
            _propertiesWriter.WriteSharedSubscriptionAvailable(packet.SharedSubscriptionAvailable);
            _propertiesWriter.WriteServerKeepAlive(packet.ServerKeepAlive);
            _propertiesWriter.WriteResponseInformation(packet.ResponseInformation);
            _propertiesWriter.WriteServerReference(packet.ServerReference);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        byte EncodeConnectPacket(MqttConnectPacket packet)
        {
            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }

            _bufferWriter.WriteString("MQTT");
            _bufferWriter.WriteByte(5); // [3.1.2.2 Protocol Version]

            byte connectFlags = 0x0;
            if (packet.CleanSession)
            {
                connectFlags |= 0x2;
            }

            if (packet.WillFlag)
            {
                connectFlags |= 0x4;
                connectFlags |= (byte)((byte)packet.WillQoS << 3);

                if (packet.WillRetain)
                {
                    connectFlags |= 0x20;
                }
            }

            if (packet.Password != null && packet.Username == null)
            {
                throw new MqttProtocolViolationException("If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].");
            }

            if (packet.Password != null)
            {
                connectFlags |= 0x40;
            }

            if (packet.Username != null)
            {
                connectFlags |= 0x80;
            }

            _bufferWriter.WriteByte(connectFlags);
            _bufferWriter.WriteTwoByteInteger(packet.KeepAlivePeriod);

            _propertiesWriter.WriteSessionExpiryInterval(packet.SessionExpiryInterval);
            _propertiesWriter.WriteAuthenticationMethod(packet.AuthenticationMethod);
            _propertiesWriter.WriteAuthenticationData(packet.AuthenticationData);
            _propertiesWriter.WriteRequestProblemInformation(packet.RequestProblemInformation);
            _propertiesWriter.WriteRequestResponseInformation(packet.RequestResponseInformation);
            _propertiesWriter.WriteReceiveMaximum(packet.ReceiveMaximum);
            _propertiesWriter.WriteTopicAliasMaximum(packet.TopicAliasMaximum);
            _propertiesWriter.WriteMaximumPacketSize(packet.MaximumPacketSize);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            _bufferWriter.WriteString(packet.ClientId);

            if (packet.WillFlag)
            {
                _propertiesWriter.WritePayloadFormatIndicator(packet.WillPayloadFormatIndicator);
                _propertiesWriter.WriteMessageExpiryInterval(packet.WillMessageExpiryInterval);
                _propertiesWriter.WriteResponseTopic(packet.WillResponseTopic);
                _propertiesWriter.WriteCorrelationData(packet.WillCorrelationData);
                _propertiesWriter.WriteContentType(packet.WillContentType);
                _propertiesWriter.WriteUserProperties(packet.WillUserProperties);
                _propertiesWriter.WriteWillDelayInterval(packet.WillDelayInterval);

                _propertiesWriter.WriteTo(_bufferWriter);
                _propertiesWriter.Reset();

                _bufferWriter.WriteString(packet.WillTopic);
                _bufferWriter.WriteBinaryData(packet.WillMessage);
            }

            if (packet.Username != null)
            {
                _bufferWriter.WriteString(packet.Username);
            }

            if (packet.Password != null)
            {
                _bufferWriter.WriteBinaryData(packet.Password);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        byte EncodeDisconnectPacket(MqttDisconnectPacket packet)
        {
            _bufferWriter.WriteByte((byte)packet.ReasonCode);

            _propertiesWriter.WriteServerReference(packet.ServerReference);
            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteSessionExpiryInterval(packet.SessionExpiryInterval);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Disconnect);
        }

        byte EncodePacket(MqttPacket packet)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket:
                    return EncodeConnectPacket(connectPacket);
                case MqttConnAckPacket connAckPacket:
                    return EncodeConnAckPacket(connAckPacket);
                case MqttDisconnectPacket disconnectPacket:
                    return EncodeDisconnectPacket(disconnectPacket);
                case MqttPingReqPacket _:
                    return EncodePingReqPacket();
                case MqttPingRespPacket _:
                    return EncodePingRespPacket();
                case MqttPublishPacket publishPacket:
                    return EncodePublishPacket(publishPacket);
                case MqttPubAckPacket pubAckPacket:
                    return EncodePubAckPacket(pubAckPacket);
                case MqttPubRecPacket pubRecPacket:
                    return EncodePubRecPacket(pubRecPacket);
                case MqttPubRelPacket pubRelPacket:
                    return EncodePubRelPacket(pubRelPacket);
                case MqttPubCompPacket pubCompPacket:
                    return EncodePubCompPacket(pubCompPacket);
                case MqttSubscribePacket subscribePacket:
                    return EncodeSubscribePacket(subscribePacket);
                case MqttSubAckPacket subAckPacket:
                    return EncodeSubAckPacket(subAckPacket);
                case MqttUnsubscribePacket unsubscribePacket:
                    return EncodeUnsubscribePacket(unsubscribePacket);
                case MqttUnsubAckPacket unsubAckPacket:
                    return EncodeUnsubAckPacket(unsubAckPacket);
                case MqttAuthPacket authPacket:
                    return EncodeAuthPacket(authPacket);

                default:
                    throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        static byte EncodePingReqPacket()
        {
            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PingReq);
        }

        static byte EncodePingRespPacket()
        {
            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PingResp);
        }

        byte EncodePubAckPacket(MqttPubAckPacket packet)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            if (_bufferWriter.Length > 0 || packet.ReasonCode != MqttPubAckReasonCode.Success)
            {
                _bufferWriter.WriteByte((byte)packet.ReasonCode);
                _propertiesWriter.WriteTo(_bufferWriter);
                _propertiesWriter.Reset();
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        byte EncodePubCompPacket(MqttPubCompPacket packet)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            if (_propertiesWriter.Length > 0 || packet.ReasonCode != MqttPubCompReasonCode.Success)
            {
                _bufferWriter.WriteByte((byte)packet.ReasonCode);
                _propertiesWriter.WriteTo(_bufferWriter);
                _propertiesWriter.Reset();
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        byte EncodePublishPacket(MqttPublishPacket packet)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }

            _bufferWriter.WriteString(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (packet.PacketIdentifier == 0)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);
            }
            else
            {
                if (packet.PacketIdentifier > 0)
                {
                    throw new MqttProtocolViolationException("Packet identifier must be 0 if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

            _propertiesWriter.WriteContentType(packet.ContentType);
            _propertiesWriter.WriteCorrelationData(packet.CorrelationData);
            _propertiesWriter.WriteMessageExpiryInterval(packet.MessageExpiryInterval);
            _propertiesWriter.WritePayloadFormatIndicator(packet.PayloadFormatIndicator);
            _propertiesWriter.WriteResponseTopic(packet.ResponseTopic);
            _propertiesWriter.WriteSubscriptionIdentifiers(packet.SubscriptionIdentifiers);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);
            _propertiesWriter.WriteTopicAlias(packet.TopicAlias);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            // The payload is the past part of the packet. But it is not added here in order to keep
            // memory allocation low.

            byte fixedHeader = 0;

            if (packet.Retain)
            {
                fixedHeader |= 0x01;
            }

            fixedHeader |= (byte)((byte)packet.QualityOfServiceLevel << 1);

            if (packet.Dup)
            {
                fixedHeader |= 0x08;
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Publish, fixedHeader);
        }

        byte EncodePubRecPacket(MqttPubRecPacket packet)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (_bufferWriter.Length > 0 || packet.ReasonCode != MqttPubRecReasonCode.Success)
            {
                _bufferWriter.WriteByte((byte)packet.ReasonCode);
                _propertiesWriter.WriteTo(_bufferWriter);
                _propertiesWriter.Reset();
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        byte EncodePubRelPacket(MqttPubRelPacket packet)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (_propertiesWriter.Length > 0 || packet.ReasonCode != MqttPubRelReasonCode.Success)
            {
                _bufferWriter.WriteByte((byte)packet.ReasonCode);
                _propertiesWriter.WriteTo(_bufferWriter);
                _propertiesWriter.Reset();
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        byte EncodeSubAckPacket(MqttSubAckPacket packet)
        {
            if (packet.ReasonCodes?.Any() != true)
            {
                throw new MqttProtocolViolationException("At least one reason code must be set[MQTT - 3.8.3 - 3].");
            }

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            foreach (var reasonCode in packet.ReasonCodes)
            {
                _bufferWriter.WriteByte((byte)reasonCode);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        byte EncodeSubscribePacket(MqttSubscribePacket packet)
        {
            if (packet.TopicFilters?.Any() != true)
            {
                throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");
            }

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (packet.SubscriptionIdentifier > 0)
            {
                _propertiesWriter.WriteSubscriptionIdentifier(packet.SubscriptionIdentifier);
            }

            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    _bufferWriter.WriteString(topicFilter.Topic);

                    var options = (byte)topicFilter.QualityOfServiceLevel;

                    if (topicFilter.NoLocal)
                    {
                        options |= 1 << 2;
                    }

                    if (topicFilter.RetainAsPublished)
                    {
                        options |= 1 << 3;
                    }

                    if (topicFilter.RetainHandling != MqttRetainHandling.SendAtSubscribe)
                    {
                        options |= (byte)((byte)topicFilter.RetainHandling << 4);
                    }

                    _bufferWriter.WriteByte(options);
                }
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        byte EncodeUnsubAckPacket(MqttUnsubAckPacket packet)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            _propertiesWriter.WriteReasonString(packet.ReasonString);
            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            foreach (var reasonCode in packet.ReasonCodes)
            {
                _bufferWriter.WriteByte((byte)reasonCode);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        byte EncodeUnsubscribePacket(MqttUnsubscribePacket packet)
        {
            if (packet.TopicFilters?.Any() != true)
            {
                throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");
            }

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            _bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            _propertiesWriter.WriteUserProperties(packet.UserProperties);

            _propertiesWriter.WriteTo(_bufferWriter);
            _propertiesWriter.Reset();

            foreach (var topicFilter in packet.TopicFilters)
            {
                _bufferWriter.WriteString(topicFilter);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        static void ThrowIfPacketIdentifierIsInvalid(ushort packetIdentifier, MqttPacket packet)
        {
            // SUBSCRIBE, UNSUBSCRIBE, and PUBLISH(in cases where QoS > 0) Control Packets MUST contain a non-zero 16 - bit Packet Identifier[MQTT - 2.3.1 - 1]. 

            if (packetIdentifier == 0)
            {
                throw new MqttProtocolViolationException($"Packet identifier is not set for {packet.GetType().Name}.");
            }
        }
    }
}