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
    public sealed class MqttV500PacketEncoder
    {
        readonly IMqttPacketWriter _packetWriter;

        public MqttV500PacketEncoder(IMqttPacketWriter packetWriter)
        {
            _packetWriter = packetWriter ?? throw new ArgumentNullException(nameof(packetWriter));
        }

        public MqttPacketBuffer Encode(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _packetWriter.Reset(5);
            _packetWriter.Seek(5);

            var fixedHeader = EncodePacket(packet, _packetWriter);
            var remainingLength = (uint)_packetWriter.Length - 5;

            var publishPacket = packet as MqttPublishPacket;
            if (publishPacket?.Payload != null)
            {
                remainingLength += (uint)publishPacket.Payload.Length;
            }
            
            var remainingLengthSize = MqttPacketWriter.GetLengthOfVariableInteger(remainingLength);
            
            var headerSize = 1 + remainingLengthSize;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginning of array (has leading 0x0)
            _packetWriter.Seek(headerOffset);
            _packetWriter.Write(fixedHeader);
            _packetWriter.WriteVariableLengthInteger(remainingLength);

            var buffer = _packetWriter.GetBuffer();

            var firstSegment = new ArraySegment<byte>(buffer, headerOffset, _packetWriter.Length - headerOffset);
            
            if (publishPacket?.Payload != null)
            {
                var payloadSegment = new ArraySegment<byte>(publishPacket.Payload, 0, publishPacket.Payload.Length);
                return new MqttPacketBuffer(firstSegment, payloadSegment);   
            }
            
            return new MqttPacketBuffer(firstSegment);
        }

        public void FreeBuffer()
        {
            _packetWriter.FreeBuffer();
        }

        static byte EncodePacket(MqttBasePacket packet, IMqttPacketWriter packetWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return EncodeConnectPacket(connectPacket, packetWriter);
                case MqttConnAckPacket connAckPacket: return EncodeConnAckPacket(connAckPacket, packetWriter);
                case MqttDisconnectPacket disconnectPacket: return EncodeDisconnectPacket(disconnectPacket, packetWriter);
                case MqttPingReqPacket _: return EncodePingReqPacket();
                case MqttPingRespPacket _: return EncodePingRespPacket();
                case MqttPublishPacket publishPacket: return EncodePublishPacket(publishPacket, packetWriter);
                case MqttPubAckPacket pubAckPacket: return EncodePubAckPacket(pubAckPacket, packetWriter);
                case MqttPubRecPacket pubRecPacket: return EncodePubRecPacket(pubRecPacket, packetWriter);
                case MqttPubRelPacket pubRelPacket: return EncodePubRelPacket(pubRelPacket, packetWriter);
                case MqttPubCompPacket pubCompPacket: return EncodePubCompPacket(pubCompPacket, packetWriter);
                case MqttSubscribePacket subscribePacket: return EncodeSubscribePacket(subscribePacket, packetWriter);
                case MqttSubAckPacket subAckPacket: return EncodeSubAckPacket(subAckPacket, packetWriter);
                case MqttUnsubscribePacket unsubscribePacket: return EncodeUnsubscribePacket(unsubscribePacket, packetWriter);
                case MqttUnsubAckPacket unsubAckPacket: return EncodeUnsubAckPacket(unsubAckPacket, packetWriter);
                case MqttAuthPacket authPacket: return EncodeAuthPacket(authPacket, packetWriter);

                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        static byte EncodeConnectPacket(MqttConnectPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }

            packetWriter.WriteWithLengthPrefix("MQTT");
            packetWriter.Write(5); // [3.1.2.2 Protocol Version]

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

            packetWriter.Write(connectFlags);
            packetWriter.Write(packet.KeepAlivePeriod);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteSessionExpiryInterval(packet.Properties.SessionExpiryInterval);
                propertiesWriter.WriteAuthenticationMethod(packet.Properties.AuthenticationMethod);
                propertiesWriter.WriteAuthenticationData(packet.Properties.AuthenticationData);
                propertiesWriter.WriteRequestProblemInformation(packet.Properties.RequestProblemInformation);
                propertiesWriter.WriteRequestResponseInformation(packet.Properties.RequestResponseInformation);
                propertiesWriter.WriteReceiveMaximum(packet.Properties.ReceiveMaximum);
                propertiesWriter.WriteTopicAliasMaximum(packet.Properties.TopicAliasMaximum);
                propertiesWriter.WriteMaximumPacketSize(packet.Properties.MaximumPacketSize);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            packetWriter.WriteWithLengthPrefix(packet.ClientId);
            
            if (packet.WillFlag)
            {
                var willPropertiesWriter = new MqttV500PropertiesWriter();
                willPropertiesWriter.WritePayloadFormatIndicator(packet.WillProperties.PayloadFormatIndicator);
                willPropertiesWriter.WriteMessageExpiryInterval(packet.WillProperties.MessageExpiryInterval);
                willPropertiesWriter.WriteResponseTopic(packet.WillProperties.ResponseTopic);
                willPropertiesWriter.WriteCorrelationData(packet.WillProperties.CorrelationData);
                willPropertiesWriter.WriteContentType(packet.WillProperties.ContentType);
                willPropertiesWriter.WriteUserProperties(packet.WillProperties.UserProperties);
                
                // This is a special case!
                willPropertiesWriter.WriteWillDelayInterval(packet.Properties?.WillDelayInterval);

                willPropertiesWriter.WriteTo(packetWriter);

                packetWriter.WriteWithLengthPrefix(packet.WillTopic);
                packetWriter.WriteWithLengthPrefix(packet.WillMessage);
            }

            if (packet.Username != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.Username);
            }

            if (packet.Password != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.Password);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        static byte EncodeConnAckPacket(MqttConnAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));
            
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            packetWriter.Write(connectAcknowledgeFlags);
            packetWriter.Write((byte)packet.ReasonCode);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteSessionExpiryInterval(packet.Properties.SessionExpiryInterval);
                propertiesWriter.WriteAuthenticationMethod(packet.Properties.AuthenticationMethod);
                propertiesWriter.WriteAuthenticationData(packet.Properties.AuthenticationData);
                propertiesWriter.WriteRetainAvailable(packet.Properties.RetainAvailable);
                propertiesWriter.WriteReceiveMaximum(packet.Properties.ReceiveMaximum);
                propertiesWriter.WriteMaximumQoS(packet.Properties.MaximumQoS);
                propertiesWriter.WriteAssignedClientIdentifier(packet.Properties.AssignedClientIdentifier);
                propertiesWriter.WriteTopicAliasMaximum(packet.Properties.TopicAliasMaximum);
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteMaximumPacketSize(packet.Properties.MaximumPacketSize);
                propertiesWriter.WriteWildcardSubscriptionAvailable(packet.Properties.WildcardSubscriptionAvailable);
                propertiesWriter.WriteSubscriptionIdentifiersAvailable(packet.Properties.SubscriptionIdentifiersAvailable);
                propertiesWriter.WriteSharedSubscriptionAvailable(packet.Properties.SharedSubscriptionAvailable);
                propertiesWriter.WriteServerKeepAlive(packet.Properties.ServerKeepAlive);
                propertiesWriter.WriteResponseInformation(packet.Properties.ResponseInformation);
                propertiesWriter.WriteServerReference(packet.Properties.ServerReference);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        static byte EncodePublishPacket(MqttPublishPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }

            packetWriter.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (packet.PacketIdentifier == 0)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                packetWriter.Write(packet.PacketIdentifier);
            }
            else
            {
                if (packet.PacketIdentifier > 0)
                {
                    throw new MqttProtocolViolationException("Packet identifier must be 0 if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteContentType(packet.Properties.ContentType);
                propertiesWriter.WriteCorrelationData(packet.Properties.CorrelationData);
                propertiesWriter.WriteMessageExpiryInterval(packet.Properties.MessageExpiryInterval);
                propertiesWriter.WritePayloadFormatIndicator(packet.Properties.PayloadFormatIndicator);
                propertiesWriter.WriteResponseTopic(packet.Properties.ResponseTopic);
                propertiesWriter.WriteSubscriptionIdentifiers(packet.Properties.SubscriptionIdentifiers);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);

                if (packet.Properties.TopicAlias > 0)
                {
                    propertiesWriter.WriteTopicAlias(packet.Properties.TopicAlias);
                }
            }

            propertiesWriter.WriteTo(packetWriter);

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

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Publish, fixedHeader);
        }

        static byte EncodePubAckPacket(MqttPubAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }
            
            packetWriter.Write(packet.PacketIdentifier);
            
            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            if (packetWriter.Length > 0 || packet.ReasonCode != MqttPubAckReasonCode.Success)
            {
                packetWriter.Write((byte)packet.ReasonCode);
                propertiesWriter.WriteTo(packetWriter);
            }
            
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        static byte EncodePubRecPacket(MqttPubRecPacket packet, IMqttPacketWriter packetWriter)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);
            
            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            packetWriter.Write(packet.PacketIdentifier);

            if (packetWriter.Length > 0 || packet.ReasonCode != MqttPubRecReasonCode.Success)
            {
                packetWriter.Write((byte)packet.ReasonCode);
                propertiesWriter.WriteTo(packetWriter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        static byte EncodePubRelPacket(MqttPubRelPacket packet, IMqttPacketWriter packetWriter)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);
            
            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            packetWriter.Write(packet.PacketIdentifier);
            
            if (propertiesWriter.Length > 0 || packet.ReasonCode != MqttPubRelReasonCode.Success)
            {
                packetWriter.Write((byte)packet.ReasonCode);
                propertiesWriter.WriteTo(packetWriter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        static byte EncodePubCompPacket(MqttPubCompPacket packet, IMqttPacketWriter packetWriter)
        {
            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);
            
            packetWriter.Write(packet.PacketIdentifier);
            
            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            if (propertiesWriter.Length > 0 || packet.ReasonCode != MqttPubCompReasonCode.Success)
            {
                packetWriter.Write((byte)packet.ReasonCode);
                propertiesWriter.WriteTo(packetWriter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        static byte EncodeSubscribePacket(MqttSubscribePacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.TopicFilters?.Any() != true) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            packetWriter.Write(packet.PacketIdentifier);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                if (packet.Properties.SubscriptionIdentifier > 0)
                {
                    propertiesWriter.WriteSubscriptionIdentifier(packet.Properties.SubscriptionIdentifier);
                }

                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter.Topic);

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
                    
                    packetWriter.Write(options);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        static byte EncodeSubAckPacket(MqttSubAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.ReasonCodes?.Any() != true) throw new MqttProtocolViolationException("At least one reason code must be set[MQTT - 3.8.3 - 3].");

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            packetWriter.Write(packet.PacketIdentifier);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            foreach (var reasonCode in packet.ReasonCodes)
            {
                packetWriter.Write((byte)reasonCode);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        static byte EncodeUnsubscribePacket(MqttUnsubscribePacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.TopicFilters?.Any() != true) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);

            packetWriter.Write(packet.PacketIdentifier);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            foreach (var topicFilter in packet.TopicFilters)
            {
                packetWriter.WriteWithLengthPrefix(topicFilter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        static byte EncodeUnsubAckPacket(MqttUnsubAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.ReasonCodes?.Any() != true) throw new MqttProtocolViolationException("At least one reason code must be set[MQTT - 3.8.3 - 3].");

            ThrowIfPacketIdentifierIsInvalid(packet.PacketIdentifier, packet);
            
            packetWriter.Write(packet.PacketIdentifier);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            foreach (var reasonCode in packet.ReasonCodes)
            {
                packetWriter.Write((byte)reasonCode);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        static byte EncodeDisconnectPacket(MqttDisconnectPacket packet, IMqttPacketWriter packetWriter)
        {
            packetWriter.Write((byte)packet.ReasonCode);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteServerReference(packet.Properties.ServerReference);
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteSessionExpiryInterval(packet.Properties.SessionExpiryInterval);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Disconnect);
        }

        static byte EncodePingReqPacket()
        {
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PingReq);
        }

        static byte EncodePingRespPacket()
        {
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PingResp);
        }

        static byte EncodeAuthPacket(MqttAuthPacket packet, IMqttPacketWriter packetWriter)
        {
            packetWriter.Write((byte)packet.ReasonCode);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteAuthenticationMethod(packet.Properties.AuthenticationMethod);
                propertiesWriter.WriteAuthenticationData(packet.Properties.AuthenticationData);
                propertiesWriter.WriteReasonString(packet.Properties.ReasonString);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            }

            propertiesWriter.WriteTo(packetWriter);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Auth);
        }
        
        static void ThrowIfPacketIdentifierIsInvalid(ushort packetIdentifier, MqttBasePacket packet)
        {
            // SUBSCRIBE, UNSUBSCRIBE, and PUBLISH(in cases where QoS > 0) Control Packets MUST contain a non-zero 16 - bit Packet Identifier[MQTT - 2.3.1 - 1]. 

            if (packetIdentifier == 0)
            {
                throw new MqttProtocolViolationException($"Packet identifier is not set for {packet.GetType().Name}.");
            }
        }
    }
}
