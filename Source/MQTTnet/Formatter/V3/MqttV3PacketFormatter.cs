// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MQTTnet.Formatter.V3
{
    public sealed class MqttV3PacketFormatter : IMqttPacketFormatter
    {
        const int FixedHeaderSize = 1;

        static readonly MqttDisconnectPacket DisconnectPacket = new MqttDisconnectPacket();

        readonly MqttBufferReader _bufferReader = new MqttBufferReader();
        readonly MqttBufferWriter _bufferWriter;
        readonly MqttProtocolVersion _mqttProtocolVersion;

        public MqttV3PacketFormatter(MqttBufferWriter bufferWriter, MqttProtocolVersion mqttProtocolVersion)
        {
            _bufferWriter = bufferWriter;
            _mqttProtocolVersion = mqttProtocolVersion;
        }

        public MqttPacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket.TotalLength == 0)
            {
                return null;
            }

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1 || controlPacketType > 14)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Publish:
                    return DecodePublishPacket(receivedMqttPacket);
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

                case MqttControlPacketType.Connect:
                    return DecodeConnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck:
                    if (_mqttProtocolVersion == MqttProtocolVersion.V311)
                    {
                        return DecodeConnAckPacketV311(receivedMqttPacket.Body);
                    }
                    else
                    {
                        return DecodeConnAckPacket(receivedMqttPacket.Body);
                    }
                case MqttControlPacketType.Disconnect:
                    return DisconnectPacket;

                case MqttControlPacketType.Subscribe:
                    return DecodeSubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck:
                    return DecodeSubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscribe:
                    return DecodeUnsubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck:
                    return DecodeUnsubAckPacket(receivedMqttPacket.Body);

                default:
                    throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        public MqttPacketBuffer Encode(MqttPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _bufferWriter.Reset(5);
            _bufferWriter.Seek(5);

            var fixedHeader = EncodePacket(packet, _bufferWriter);
            var remainingLength = (uint)(_bufferWriter.Length - 5);

            ReadOnlySequence<byte> payload = default;
            if (packet is MqttPublishPacket publishPacket)
            {
                payload = publishPacket.Payload;
                remainingLength += (uint)payload.Length;
            }
            else
            {
                publishPacket = null;
            }

            var remainingLengthSize = MqttBufferWriter.GetVariableByteIntegerSize(remainingLength);

            var headerSize = FixedHeaderSize + remainingLengthSize;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginning of array (has leading 0x0)
            _bufferWriter.Seek(headerOffset);
            _bufferWriter.WriteByte(fixedHeader);
            _bufferWriter.WriteVariableByteInteger(remainingLength);

            var firstSegment = _bufferWriter.GetWrittenMemory()[headerOffset..];

            return payload.Length == 0
                ? new MqttPacketBuffer(firstSegment)
                : new MqttPacketBuffer(firstSegment, payload);
        }

        MqttPacket DecodeConnAckPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var packet = new MqttConnAckPacket();

            _bufferReader.ReadByte(); // Reserved.
            packet.ReturnCode = (MqttConnectReturnCode)_bufferReader.ReadByte();

            return packet;
        }

        MqttPacket DecodeConnAckPacketV311(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = _bufferReader.ReadByte();

            packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            packet.ReturnCode = (MqttConnectReturnCode)_bufferReader.ReadByte();

            return packet;
        }

        MqttPacket DecodeConnectPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var protocolName = _bufferReader.ReadString();
            var protocolVersion = _bufferReader.ReadByte();

            if (protocolName != "MQTT" && protocolName != "MQIsdp")
            {
                throw new MqttProtocolViolationException("MQTT protocol name do not match MQTT v3.");
            }

            var tryPrivate = (protocolVersion & 0x80) > 0;
            protocolVersion &= 0x7F;

            if (protocolVersion != 3 && protocolVersion != 4)
            {
                throw new MqttProtocolViolationException("MQTT protocol version do not match MQTT v3.");
            }

            var packet = new MqttConnectPacket
            {
                TryPrivate = tryPrivate
            };

            var connectFlags = _bufferReader.ReadByte();
            if ((connectFlags & 0x1) > 0)
            {
                throw new MqttProtocolViolationException("The first bit of the Connect Flags must be set to 0.");
            }

            packet.CleanSession = (connectFlags & 0x2) > 0;

            var willFlag = (connectFlags & 0x4) > 0;
            var willQoS = (connectFlags & 0x18) >> 3;
            var willRetain = (connectFlags & 0x20) > 0;
            var passwordFlag = (connectFlags & 0x40) > 0;
            var usernameFlag = (connectFlags & 0x80) > 0;

            packet.KeepAlivePeriod = _bufferReader.ReadTwoByteInteger();
            packet.ClientId = _bufferReader.ReadString();

            if (willFlag)
            {
                packet.WillFlag = true;
                packet.WillQoS = (MqttQualityOfServiceLevel)willQoS;
                packet.WillRetain = willRetain;

                packet.WillTopic = _bufferReader.ReadString();
                packet.WillMessage = _bufferReader.ReadBinaryData().Join();
            }

            if (usernameFlag)
            {
                packet.Username = _bufferReader.ReadString();
            }

            if (passwordFlag)
            {
                packet.Password = _bufferReader.ReadBinaryData().ToArray();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        MqttPacket DecodePubAckPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };
        }

        MqttPacket DecodePubCompPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };
        }

        MqttPacket DecodePublishPacket(ReceivedMqttPacket receivedMqttPacket)
        {
            ThrowIfBodyIsEmpty(receivedMqttPacket.Body.Length);

            _bufferReader.SetBuffer(receivedMqttPacket.Body);

            var retain = (receivedMqttPacket.FixedHeader & 0x1) > 0;
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)((receivedMqttPacket.FixedHeader >> 1) & 0x3);
            var dup = (receivedMqttPacket.FixedHeader & 0x8) > 0;

            var topic = _bufferReader.ReadString();

            ushort packetIdentifier = 0;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = _bufferReader.ReadTwoByteInteger();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            if (!_bufferReader.EndOfStream)
            {
                packet.Payload = _bufferReader.ReadRemainingData();
            }

            return packet;
        }

        MqttPacket DecodePubRecPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };
        }

        MqttPacket DecodePubRelPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };
        }

        MqttPacket DecodeSubAckPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger(),
                ReasonCodes = new List<MqttSubscribeReasonCode>((int)_bufferReader.BytesLeft)
            };

            while (!_bufferReader.EndOfStream)
            {
                packet.ReasonCodes.Add((MqttSubscribeReasonCode)_bufferReader.ReadByte());
            }

            return packet;
        }

        MqttPacket DecodeSubscribePacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            while (!_bufferReader.EndOfStream)
            {
                var topicFilter = new MqttTopicFilter
                {
                    Topic = _bufferReader.ReadString(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)_bufferReader.ReadByte()
                };

                packet.TopicFilters.Add(topicFilter);
            }

            return packet;
        }

        MqttPacket DecodeUnsubAckPacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };
        }

        MqttPacket DecodeUnsubscribePacket(ReadOnlySequence<byte> body)
        {
            ThrowIfBodyIsEmpty(body.Length);

            _bufferReader.SetBuffer(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = _bufferReader.ReadTwoByteInteger()
            };

            while (!_bufferReader.EndOfStream)
            {
                packet.TopicFilters.Add(_bufferReader.ReadString());
            }

            return packet;
        }

        byte EncodeConnAckPacket(MqttConnAckPacket packet, MqttBufferWriter bufferWriter)
        {
            bufferWriter.WriteByte(0); // Reserved.
            bufferWriter.WriteByte((byte)packet.ReturnCode);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        byte EncodeConnAckPacketV311(MqttConnAckPacket packet, MqttBufferWriter bufferWriter)
        {
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            bufferWriter.WriteByte(connectAcknowledgeFlags);
            bufferWriter.WriteByte((byte)packet.ReturnCode);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        byte EncodeConnectPacket(MqttConnectPacket packet, MqttBufferWriter bufferWriter)
        {
            ValidateConnectPacket(packet);

            bufferWriter.WriteString("MQIsdp");

            var protocolVersion = 3;
            if (packet.TryPrivate)
            {
                protocolVersion |= 0x80;
            }

            bufferWriter.WriteByte((byte)protocolVersion);

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

            bufferWriter.WriteByte(connectFlags);
            bufferWriter.WriteTwoByteInteger(packet.KeepAlivePeriod);
            bufferWriter.WriteString(packet.ClientId);

            if (packet.WillFlag)
            {
                bufferWriter.WriteString(packet.WillTopic);
                bufferWriter.WriteBinary(packet.WillMessage.Span);
            }

            if (packet.Username != null)
            {
                bufferWriter.WriteString(packet.Username);
            }

            if (packet.Password != null)
            {
                bufferWriter.WriteBinary(packet.Password);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        byte EncodeConnectPacketV311(MqttConnectPacket packet, MqttBufferWriter bufferWriter)
        {
            ValidateConnectPacket(packet);

            bufferWriter.WriteString("MQTT");
            bufferWriter.WriteByte(4); // 3.1.2.2 Protocol Level 4

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

            bufferWriter.WriteByte(connectFlags);
            bufferWriter.WriteTwoByteInteger(packet.KeepAlivePeriod);
            bufferWriter.WriteString(packet.ClientId);

            if (packet.WillFlag)
            {
                bufferWriter.WriteString(packet.WillTopic);
                bufferWriter.WriteBinary(packet.WillMessage.Span);
            }

            if (packet.Username != null)
            {
                bufferWriter.WriteString(packet.Username);
            }

            if (packet.Password.Length > 0)
            {
                bufferWriter.WriteBinary(packet.Password);
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        static byte EncodeEmptyPacket(MqttControlPacketType type)
        {
            return MqttBufferWriter.BuildFixedHeader(type);
        }

        byte EncodePacket(MqttPacket packet, MqttBufferWriter bufferWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket:
                    if (_mqttProtocolVersion == MqttProtocolVersion.V311)
                    {
                        return EncodeConnectPacketV311(connectPacket, bufferWriter);
                    }
                    else
                    {
                        return EncodeConnectPacket(connectPacket, bufferWriter);
                    }
                case MqttConnAckPacket connAckPacket:
                    if (_mqttProtocolVersion == MqttProtocolVersion.V311)
                    {
                        return EncodeConnAckPacketV311(connAckPacket, bufferWriter);
                    }
                    else
                    {
                        return EncodeConnAckPacket(connAckPacket, bufferWriter);
                    }
                case MqttDisconnectPacket _:
                    return EncodeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _:
                    return EncodeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _:
                    return EncodeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket:
                    return EncodePublishPacket(publishPacket, bufferWriter);
                case MqttPubAckPacket pubAckPacket:
                    return EncodePubAckPacket(pubAckPacket, bufferWriter);
                case MqttPubRecPacket pubRecPacket:
                    return EncodePubRecPacket(pubRecPacket, bufferWriter);
                case MqttPubRelPacket pubRelPacket:
                    return EncodePubRelPacket(pubRelPacket, bufferWriter);
                case MqttPubCompPacket pubCompPacket:
                    return EncodePubCompPacket(pubCompPacket, bufferWriter);
                case MqttSubscribePacket subscribePacket:
                    return EncodeSubscribePacket(subscribePacket, bufferWriter);
                case MqttSubAckPacket subAckPacket:
                    return EncodeSubAckPacket(subAckPacket, bufferWriter);
                case MqttUnsubscribePacket unsubscribePacket:
                    return EncodeUnsubscribePacket(unsubscribePacket, bufferWriter);
                case MqttUnsubAckPacket unsubAckPacket:
                    return EncodeUnsubAckPacket(unsubAckPacket, bufferWriter);

                default:
                    throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        static byte EncodePubAckPacket(MqttPubAckPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        static byte EncodePubCompPacket(MqttPubCompPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        static byte EncodePublishPacket(MqttPublishPacket packet, MqttBufferWriter bufferWriter)
        {
            ValidatePublishPacket(packet);

            bufferWriter.WriteString(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (packet.PacketIdentifier == 0)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);
            }
            else
            {
                if (packet.PacketIdentifier > 0)
                {
                    throw new MqttProtocolViolationException("Packet identifier must be empty if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

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

        static byte EncodePubRecPacket(MqttPubRecPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        static byte EncodePubRelPacket(MqttPubRelPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        static byte EncodeSubAckPacket(MqttSubAckPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("SubAck packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (packet.ReasonCodes.Any())
            {
                foreach (var packetSubscribeReturnCode in packet.ReasonCodes)
                {
                    if (packetSubscribeReturnCode == MqttSubscribeReasonCode.GrantedQoS0)
                    {
                        bufferWriter.WriteByte((byte)MqttSubscribeReturnCode.SuccessMaximumQoS0);
                    }
                    else if (packetSubscribeReturnCode == MqttSubscribeReasonCode.GrantedQoS1)
                    {
                        bufferWriter.WriteByte((byte)MqttSubscribeReturnCode.SuccessMaximumQoS1);
                    }
                    else if (packetSubscribeReturnCode == MqttSubscribeReasonCode.GrantedQoS2)
                    {
                        bufferWriter.WriteByte((byte)MqttSubscribeReturnCode.SuccessMaximumQoS2);
                    }
                    else
                    {
                        bufferWriter.WriteByte((byte)MqttSubscribeReturnCode.Failure);
                    }
                }
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        static byte EncodeSubscribePacket(MqttSubscribePacket packet, MqttBufferWriter bufferWriter)
        {
            if (!packet.TopicFilters.Any())
            {
                throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");
            }

            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("Subscribe packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    bufferWriter.WriteString(topicFilter.Topic);
                    bufferWriter.WriteByte((byte)topicFilter.QualityOfServiceLevel);
                }
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        static byte EncodeUnsubAckPacket(MqttUnsubAckPacket packet, MqttBufferWriter bufferWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("UnsubAck packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);
            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        static byte EncodeUnsubscribePacket(MqttUnsubscribePacket packet, MqttBufferWriter bufferWriter)
        {
            if (!packet.TopicFilters.Any())
            {
                throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");
            }

            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("Unsubscribe packet has no packet identifier.");
            }

            bufferWriter.WriteTwoByteInteger(packet.PacketIdentifier);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    bufferWriter.WriteString(topicFilter);
                }
            }

            return MqttBufferWriter.BuildFixedHeader(MqttControlPacketType.Unsubscribe, 0x02);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ThrowIfBodyIsEmpty(long bodyLength)
        {
            if (bodyLength == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }

        void ValidateConnectPacket(MqttConnectPacket packet)
        {
            ArgumentNullException.ThrowIfNull(packet);

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        static void ValidatePublishPacket(MqttPublishPacket packet)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }
        }
    }
}