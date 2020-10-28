using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V3
{
    public class MqttV310PacketFormatter : IMqttPacketFormatter
    {
        const int FixedHeaderSize = 1;

        static readonly MqttPingReqPacket PingReqPacket = new MqttPingReqPacket();
        static readonly MqttPingRespPacket PingRespPacket = new MqttPingRespPacket();
        static readonly MqttDisconnectPacket DisconnectPacket = new MqttDisconnectPacket();

        readonly IMqttPacketWriter _packetWriter;

        public MqttV310PacketFormatter(IMqttPacketWriter packetWriter)
        {
            _packetWriter = packetWriter;
        }

        public IMqttDataConverter DataConverter { get; } = new MqttV310DataConverter();

        public ArraySegment<byte> Encode(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _packetWriter.Reset(5);
            _packetWriter.Seek(5);

            var fixedHeader = EncodePacket(packet, _packetWriter);
            var remainingLength = (uint)(_packetWriter.Length - 5);

            var remainingLengthSize = MqttPacketWriter.GetLengthOfVariableInteger(remainingLength);

            var headerSize = FixedHeaderSize + remainingLengthSize;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginning of array (has leading 0x0)
            _packetWriter.Seek(headerOffset);
            _packetWriter.Write(fixedHeader);
            _packetWriter.WriteVariableLengthInteger(remainingLength);

            var buffer = _packetWriter.GetBuffer();

            return new ArraySegment<byte>(buffer, headerOffset, _packetWriter.Length - headerOffset);
        }

        public MqttBasePacket Decode(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1 || controlPacketType > 14)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect: return DecodeConnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck: return DecodeConnAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Disconnect: return DisconnectPacket;
                case MqttControlPacketType.Publish: return DecodePublishPacket(receivedMqttPacket);
                case MqttControlPacketType.PubAck: return DecodePubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRec: return DecodePubRecPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRel: return DecodePubRelPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PubComp: return DecodePubCompPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.PingReq: return PingReqPacket;
                case MqttControlPacketType.PingResp: return PingRespPacket;
                case MqttControlPacketType.Subscribe: return DecodeSubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck: return DecodeSubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscibe: return DecodeUnsubscribePacket(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck: return DecodeUnsubAckPacket(receivedMqttPacket.Body);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        public void FreeBuffer()
        {
            _packetWriter.FreeBuffer();
        }

        byte EncodePacket(MqttBasePacket packet, IMqttPacketWriter packetWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return EncodeConnectPacket(connectPacket, packetWriter);
                case MqttConnAckPacket connAckPacket: return EncodeConnAckPacket(connAckPacket, packetWriter);
                case MqttDisconnectPacket _: return EncodeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return EncodeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return EncodeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return EncodePublishPacket(publishPacket, packetWriter);
                case MqttPubAckPacket pubAckPacket: return EncodePubAckPacket(pubAckPacket, packetWriter);
                case MqttPubRecPacket pubRecPacket: return EncodePubRecPacket(pubRecPacket, packetWriter);
                case MqttPubRelPacket pubRelPacket: return EncodePubRelPacket(pubRelPacket, packetWriter);
                case MqttPubCompPacket pubCompPacket: return EncodePubCompPacket(pubCompPacket, packetWriter);
                case MqttSubscribePacket subscribePacket: return EncodeSubscribePacket(subscribePacket, packetWriter);
                case MqttSubAckPacket subAckPacket: return EncodeSubAckPacket(subAckPacket, packetWriter);
                case MqttUnsubscribePacket unsubscribePacket: return EncodeUnsubscribePacket(unsubscribePacket, packetWriter);
                case MqttUnsubAckPacket unsubAckPacket: return EncodeUnsubAckPacket(unsubAckPacket, packetWriter);

                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        static MqttBasePacket DecodeUnsubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        static MqttBasePacket DecodePubCompPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        static MqttBasePacket DecodePubRelPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        static MqttBasePacket DecodePubRecPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        static MqttBasePacket DecodePubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        static MqttBasePacket DecodeUnsubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger(),
            };

            while (!body.EndOfStream)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        static MqttBasePacket DecodeSubscribePacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            while (!body.EndOfStream)
            {
                var topicFilter = new MqttTopicFilter
                {
                    Topic = body.ReadStringWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)body.ReadByte()
                };

                packet.TopicFilters.Add(topicFilter);
            }

            return packet;
        }

        static MqttBasePacket DecodePublishPacket(ReceivedMqttPacket receivedMqttPacket)
        {
            ThrowIfBodyIsEmpty(receivedMqttPacket.Body);

            var retain = (receivedMqttPacket.FixedHeader & 0x1) > 0;
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)(receivedMqttPacket.FixedHeader >> 1 & 0x3);
            var dup = (receivedMqttPacket.FixedHeader & 0x8) > 0;

            var topic = receivedMqttPacket.Body.ReadStringWithLengthPrefix();

            ushort packetIdentifier = 0;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = receivedMqttPacket.Body.ReadTwoByteInteger();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            if (!receivedMqttPacket.Body.EndOfStream)
            {
                packet.Payload = receivedMqttPacket.Body.ReadRemainingData();
            }

            return packet;
        }

        MqttBasePacket DecodeConnectPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var protocolName = body.ReadStringWithLengthPrefix();
            var protocolVersion = body.ReadByte();

            if (protocolName != "MQTT" && protocolName != "MQIsdp")
            {
                throw new MqttProtocolViolationException("MQTT protocol name do not match MQTT v3.");
            }

            if (protocolVersion != 3 && protocolVersion != 4)
            {
                throw new MqttProtocolViolationException("MQTT protocol version do not match MQTT v3.");
            }

            var packet = new MqttConnectPacket();

            var connectFlags = body.ReadByte();
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

            packet.KeepAlivePeriod = body.ReadTwoByteInteger();
            packet.ClientId = body.ReadStringWithLengthPrefix();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage
                {
                    Topic = body.ReadStringWithLengthPrefix(),
                    Payload = body.ReadWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)willQoS,
                    Retain = willRetain
                };
            }

            if (usernameFlag)
            {
                packet.Username = body.ReadStringWithLengthPrefix();
            }

            if (passwordFlag)
            {
                packet.Password = body.ReadWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        static MqttBasePacket DecodeSubAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            while (!body.EndOfStream)
            {
                packet.ReturnCodes.Add((MqttSubscribeReturnCode)body.ReadByte());
            }

            return packet;
        }

        protected virtual MqttBasePacket DecodeConnAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            body.ReadByte(); // Reserved.
            packet.ReturnCode = (MqttConnectReturnCode)body.ReadByte();

            return packet;
        }

        protected void ValidateConnectPacket(MqttConnectPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

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

        protected virtual byte EncodeConnectPacket(MqttConnectPacket packet, IMqttPacketWriter packetWriter)
        {
            ValidateConnectPacket(packet);

            packetWriter.WriteWithLengthPrefix("MQIsdp");
            packetWriter.Write(3); // Protocol Level 3

            byte connectFlags = 0x0;
            if (packet.CleanSession)
            {
                connectFlags |= 0x2;
            }

            if (packet.WillMessage != null)
            {
                connectFlags |= 0x4;
                connectFlags |= (byte)((byte)packet.WillMessage.QualityOfServiceLevel << 3);

                if (packet.WillMessage.Retain)
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
            packetWriter.WriteWithLengthPrefix(packet.ClientId);

            if (packet.WillMessage != null)
            {
                packetWriter.WriteWithLengthPrefix(packet.WillMessage.Topic);
                packetWriter.WriteWithLengthPrefix(packet.WillMessage.Payload);
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

        protected virtual byte EncodeConnAckPacket(MqttConnAckPacket packet, IMqttPacketWriter packetWriter)
        {
            packetWriter.Write(0); // Reserved.
            packetWriter.Write((byte)packet.ReturnCode.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        static byte EncodePubRelPacket(MqttPubRelPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        static byte EncodePublishPacket(MqttPublishPacket packet, IMqttPacketWriter packetWriter)
        {
            ValidatePublishPacket(packet);

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
                    throw new MqttProtocolViolationException("Packet identifier must be empty if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

            if (packet.Payload?.Length > 0)
            {
                packetWriter.Write(packet.Payload, 0, packet.Payload.Length);
            }

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
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        static byte EncodePubRecPacket(MqttPubRecPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        static byte EncodePubCompPacket(MqttPubCompPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        static byte EncodeSubscribePacket(MqttSubscribePacket packet, IMqttPacketWriter packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("Subscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter.Topic);
                    packetWriter.Write((byte)topicFilter.QualityOfServiceLevel);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        static byte EncodeSubAckPacket(MqttSubAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("SubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            if (packet.ReturnCodes?.Any() == true)
            {
                foreach (var packetSubscribeReturnCode in packet.ReturnCodes)
                {
                    packetWriter.Write((byte)packetSubscribeReturnCode);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        static byte EncodeUnsubscribePacket(MqttUnsubscribePacket packet, IMqttPacketWriter packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("Unsubscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        static byte EncodeUnsubAckPacket(MqttUnsubAckPacket packet, IMqttPacketWriter packetWriter)
        {
            if (packet.PacketIdentifier == 0)
            {
                throw new MqttProtocolViolationException("UnsubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier);
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        static byte EncodeEmptyPacket(MqttControlPacketType type)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ThrowIfBodyIsEmpty(IMqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
