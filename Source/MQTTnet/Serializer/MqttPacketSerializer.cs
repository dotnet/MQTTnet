using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Linq;
using MQTTnet.Adapter;

namespace MQTTnet.Serializer
{
    public class MqttPacketSerializer : IMqttPacketSerializer
    {
        private const int FixedHeaderSize = 1;

        private readonly MqttPacketWriter _packetWriter = new MqttPacketWriter();

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public ArraySegment<byte> Serialize(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _packetWriter.Reset();
            _packetWriter.Seek(5);

            var fixedHeader = SerializePacket(packet, _packetWriter);
            var remainingLength = _packetWriter.Length - 5;

            var remainingLengthBuffer = MqttPacketWriter.EncodeRemainingLength(remainingLength);

            var headerSize = FixedHeaderSize + remainingLengthBuffer.Count;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginining of array (has leading 0x0)
            _packetWriter.Seek(headerOffset);
            _packetWriter.Write(fixedHeader);
            _packetWriter.Write(remainingLengthBuffer.Array, remainingLengthBuffer.Offset, remainingLengthBuffer.Count);

            var buffer = _packetWriter.GetBuffer();
            return new ArraySegment<byte>(buffer, headerOffset, _packetWriter.Length - headerOffset);
        }

        public MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            var controlPacketType = receivedMqttPacket.FixedHeader >> 4;
            if (controlPacketType < 1 || controlPacketType > 14)
            {
                throw new MqttProtocolViolationException($"The packet type is invalid ({controlPacketType}).");
            }

            switch ((MqttControlPacketType)controlPacketType)
            {
                case MqttControlPacketType.Connect: return DeserializeConnect(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck: return DeserializeConnAck(receivedMqttPacket.Body);
                case MqttControlPacketType.Disconnect: return new MqttDisconnectPacket();
                case MqttControlPacketType.Publish: return DeserializePublish(receivedMqttPacket);
                case MqttControlPacketType.PubAck: return DeserializePubAck(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRec: return DeserializePubRec(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRel: return DeserializePubRel(receivedMqttPacket.Body);
                case MqttControlPacketType.PubComp: return DeserializePubComp(receivedMqttPacket.Body);
                case MqttControlPacketType.PingReq: return new MqttPingReqPacket();
                case MqttControlPacketType.PingResp: return new MqttPingRespPacket();
                case MqttControlPacketType.Subscribe: return DeserializeSubscribe(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck: return DeserializeSubAck(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscibe: return DeserializeUnsubscribe(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck: return DeserializeUnsubAck(receivedMqttPacket.Body);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        public void FreeBuffer()
        {
            _packetWriter.FreeBuffer();
        }

        private byte SerializePacket(MqttBasePacket packet, MqttPacketWriter packetWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return Serialize(connectPacket, packetWriter);
                case MqttConnAckPacket connAckPacket: return Serialize(connAckPacket, packetWriter);
                case MqttDisconnectPacket _: return SerializeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return Serialize(publishPacket, packetWriter);
                case MqttPubAckPacket pubAckPacket: return Serialize(pubAckPacket, packetWriter);
                case MqttPubRecPacket pubRecPacket: return Serialize(pubRecPacket, packetWriter);
                case MqttPubRelPacket pubRelPacket: return Serialize(pubRelPacket, packetWriter);
                case MqttPubCompPacket pubCompPacket: return Serialize(pubCompPacket, packetWriter);
                case MqttSubscribePacket subscribePacket: return Serialize(subscribePacket, packetWriter);
                case MqttSubAckPacket subAckPacket: return Serialize(subAckPacket, packetWriter);
                case MqttUnsubscribePacket unsubscribePacket: return Serialize(unsubscribePacket, packetWriter);
                case MqttUnsubAckPacket unsubAckPacket: return Serialize(unsubAckPacket, packetWriter);
                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        private static MqttBasePacket DeserializeUnsubAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubComp(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRel(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRec(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializeUnsubscribe(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadUInt16(),
            };

            while (!body.EndOfStream)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DeserializeSubscribe(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadUInt16()
            };

            while (!body.EndOfStream)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    body.ReadStringWithLengthPrefix(),
                    (MqttQualityOfServiceLevel)body.ReadByte()));
            }

            return packet;
        }

        private static MqttBasePacket DeserializePublish(ReceivedMqttPacket receivedMqttPacket)
        {
            ThrowIfBodyIsEmpty(receivedMqttPacket.Body);

            var retain = (receivedMqttPacket.FixedHeader & 0x1) > 0;
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)(receivedMqttPacket.FixedHeader >> 1 & 0x3);
            var dup = (receivedMqttPacket.FixedHeader & 0x8) > 0;

            var topic = receivedMqttPacket.Body.ReadStringWithLengthPrefix();

            ushort? packetIdentifier = null;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = receivedMqttPacket.Body.ReadUInt16();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                Payload = receivedMqttPacket.Body.ReadRemainingData().ToArray(),
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            return packet;
        }

        private static MqttBasePacket DeserializeConnect(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var protocolName = body.ReadStringWithLengthPrefix();

            MqttProtocolVersion protocolVersion;
            if (protocolName == "MQTT")
            {
                var protocolLevel = body.ReadByte();
                if (protocolLevel != 4)
                {
                    throw new MqttProtocolViolationException($"Protocol level ({protocolLevel}) not supported for MQTT 3.1.1.");
                }

                protocolVersion = MqttProtocolVersion.V311;
            }
            else if (protocolName == "MQIsdp")
            {
                var protocolLevel = body.ReadByte();
                if (protocolLevel != 3)
                {
                    throw new MqttProtocolViolationException($"Protocol level ({protocolLevel}) not supported for MQTT 3.1.");
                }

                protocolVersion = MqttProtocolVersion.V310;
            }
            else
            {
                throw new MqttProtocolViolationException($"Protocol name ({protocolName}) is not supported.");
            }

            var connectFlags = body.ReadByte();
            if ((connectFlags & 0x1) > 0)
            {
                throw new MqttProtocolViolationException("The first bit of the Connect Flags must be set to 0.");
            }

            var packet = new MqttConnectPacket
            {
                ProtocolVersion = protocolVersion,
                CleanSession = (connectFlags & 0x2) > 0
            };

            var willFlag = (connectFlags & 0x4) > 0;
            var willQoS = (connectFlags & 0x18) >> 3;
            var willRetain = (connectFlags & 0x20) > 0;
            var passwordFlag = (connectFlags & 0x40) > 0;
            var usernameFlag = (connectFlags & 0x80) > 0;

            packet.KeepAlivePeriod = body.ReadUInt16();
            packet.ClientId = body.ReadStringWithLengthPrefix();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage
                {
                    Topic = body.ReadStringWithLengthPrefix(),
                    Payload = body.ReadWithLengthPrefix().ToArray(),
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
                packet.Password = body.ReadStringWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private static MqttBasePacket DeserializeSubAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };

            while (!body.EndOfStream)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)body.ReadByte());
            }

            return packet;
        }

        private MqttBasePacket DeserializeConnAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = body.ReadByte();

            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            }

            packet.ConnectReturnCode = (MqttConnectReturnCode)body.ReadByte();

            return packet;
        }

        private static void ValidateConnectPacket(MqttConnectPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ValidatePublishPacket(MqttPublishPacket packet)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }
        }

        private byte Serialize(MqttConnectPacket packet, MqttPacketWriter packetWriter)
        {
            ValidateConnectPacket(packet);

            // Write variable header
            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                packetWriter.WriteWithLengthPrefix("MQTT");
                packetWriter.Write(4); // 3.1.2.2 Protocol Level 4
            }
            else
            {
                packetWriter.WriteWithLengthPrefix("MQIsdp");
                packetWriter.Write(3); // Protocol Level 3
            }

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

        private byte Serialize(MqttConnAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (ProtocolVersion == MqttProtocolVersion.V310)
            {
                packetWriter.Write(0);
            }
            else if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                byte connectAcknowledgeFlags = 0x0;
                if (packet.IsSessionPresent)
                {
                    connectAcknowledgeFlags |= 0x1;
                }

                packetWriter.Write(connectAcknowledgeFlags);
            }
            else
            {
                throw new MqttProtocolViolationException("Protocol version not supported.");
            }

            packetWriter.Write((byte)packet.ConnectReturnCode);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static byte Serialize(MqttPubRelPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        private static byte Serialize(MqttPublishPacket packet, MqttPacketWriter packetWriter)
        {
            ValidatePublishPacket(packet);

            packetWriter.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (!packet.PacketIdentifier.HasValue)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                packetWriter.Write(packet.PacketIdentifier.Value);
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

        private static byte Serialize(MqttPubAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte Serialize(MqttPubRecPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte Serialize(MqttPubCompPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte Serialize(MqttSubscribePacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Subscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

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

        private static byte Serialize(MqttSubAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("SubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            if (packet.SubscribeReturnCodes?.Any() == true)
            {
                foreach (var packetSubscribeReturnCode in packet.SubscribeReturnCodes)
                {
                    packetWriter.Write((byte)packetSubscribeReturnCode);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        private static byte Serialize(MqttUnsubscribePacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Unsubscribe packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    packetWriter.WriteWithLengthPrefix(topicFilter);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        private static byte Serialize(MqttUnsubAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("UnsubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        private static byte SerializeEmptyPacket(MqttControlPacketType type)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowIfBodyIsEmpty(MqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
