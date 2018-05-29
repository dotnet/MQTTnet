﻿using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.IO;
using System.Linq;
using MQTTnet.Adapter;

namespace MQTTnet.Serializer
{
    public sealed class MqttPacketSerializer : IMqttPacketSerializer
    {
        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public ArraySegment<byte> Serialize(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            using (var stream = new MemoryStream(128))
            {
                // Leave enough head space for max header size (fixed + 4 variable remaining length)
                stream.Position = 5;
                var fixedHeader = SerializePacket(packet, stream);

                stream.Position = 1;
                var remainingLength = MqttPacketWriter.EncodeRemainingLength((int)stream.Length - 5, stream);

                var headerSize = remainingLength + 1;
                var headerOffset = 5 - headerSize;

                // Position cursor on correct offset on beginining of array (has leading 0x0)
                stream.Position = headerOffset;

                stream.WriteByte(fixedHeader);

#if NET461 || NET452 || NETSTANDARD2_0
                var buffer = stream.GetBuffer();
#else
                var buffer = stream.ToArray();
#endif
                return new ArraySegment<byte>(buffer, headerOffset, (int)stream.Length - headerOffset);
            }
        }

        private byte SerializePacket(MqttBasePacket packet, Stream stream)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return Serialize(connectPacket, stream);
                case MqttConnAckPacket connAckPacket: return Serialize(connAckPacket, stream);
                case MqttDisconnectPacket _: return SerializeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return Serialize(publishPacket, stream);
                case MqttPubAckPacket pubAckPacket: return Serialize(pubAckPacket, stream);
                case MqttPubRecPacket pubRecPacket: return Serialize(pubRecPacket, stream);
                case MqttPubRelPacket pubRelPacket: return Serialize(pubRelPacket, stream);
                case MqttPubCompPacket pubCompPacket: return Serialize(pubCompPacket, stream);
                case MqttSubscribePacket subscribePacket: return Serialize(subscribePacket, stream);
                case MqttSubAckPacket subAckPacket: return Serialize(subAckPacket, stream);
                case MqttUnsubscribePacket unsubscribePacket: return Serialize(unsubscribePacket, stream);
                case MqttUnsubAckPacket unsubAckPacket: return Serialize(unsubAckPacket, stream);
                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
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

        private static MqttBasePacket DeserializeUnsubAck(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubComp(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRel(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRec(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubAck(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializeUnsubscribe(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = body.ReadUInt16(),
            };

            while (body.Position != body.Length)
            {
                packet.TopicFilters.Add(body.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DeserializeSubscribe(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadUInt16()
            };

            while (body.Position != body.Length)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    body.ReadStringWithLengthPrefix(),
                    (MqttQualityOfServiceLevel)body.ReadByte()));
            }

            return packet;
        }

        private static MqttBasePacket DeserializePublish(ReceivedMqttPacket receivedMqttPacket)
        {
            var body = receivedMqttPacket.Body;
            ThrowIfBodyIsEmpty(body);

            var fixedHeader = new ByteReader(receivedMqttPacket.FixedHeader);
            var retain = fixedHeader.Read();
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)fixedHeader.Read(2);
            var dup = fixedHeader.Read();

            var topic = body.ReadStringWithLengthPrefix();

            ushort? packetIdentifier = null;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = body.ReadUInt16();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                Payload = body.ReadRemainingData(),
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            return packet;
        }

        private static MqttBasePacket DeserializeConnect(Stream body)
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

            var connectFlags = new ByteReader(body.ReadByte());
            if (connectFlags.Read())
            {
                throw new MqttProtocolViolationException("The first bit of the Connect Flags must be set to 0.");
            }

            var packet = new MqttConnectPacket
            {
                ProtocolVersion = protocolVersion,
                CleanSession = connectFlags.Read()
            };

            var willFlag = connectFlags.Read();
            var willQoS = connectFlags.Read(2);
            var willRetain = connectFlags.Read();
            var passwordFlag = connectFlags.Read();
            var usernameFlag = connectFlags.Read();

            packet.KeepAlivePeriod = body.ReadUInt16();
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
                packet.Password = body.ReadStringWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private static MqttBasePacket DeserializeSubAck(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadUInt16()
            };

            while (body.Position != body.Length)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)body.ReadByte());
            }

            return packet;
        }

        private MqttBasePacket DeserializeConnAck(Stream body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            var firstByteReader = new ByteReader(body.ReadByte());

            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                packet.IsSessionPresent = firstByteReader.Read();
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

        private static void ValidatePublishPacket(MqttPublishPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }
        }

        private byte Serialize(MqttConnectPacket packet, Stream stream)
        {
            ValidateConnectPacket(packet);

            // Write variable header
            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                stream.WriteWithLengthPrefix("MQTT");
                stream.WriteByte(4); // 3.1.2.2 Protocol Level 4
            }
            else
            {
                stream.WriteWithLengthPrefix("MQIsdp");
                stream.WriteByte(3); // Protocol Level 3
            }

            var connectFlags = new ByteWriter(); // 3.1.2.3 Connect Flags
            connectFlags.Write(false); // Reserved
            connectFlags.Write(packet.CleanSession);
            connectFlags.Write(packet.WillMessage != null);

            if (packet.WillMessage != null)
            {
                connectFlags.Write((int)packet.WillMessage.QualityOfServiceLevel, 2);
                connectFlags.Write(packet.WillMessage.Retain);
            }
            else
            {
                connectFlags.Write(0, 2);
                connectFlags.Write(false);
            }

            if (packet.Password != null && packet.Username == null)
            {
                throw new MqttProtocolViolationException("If the User Name Flag is set to 0, the Password Flag MUST be set to 0 [MQTT-3.1.2-22].");
            }

            connectFlags.Write(packet.Password != null);
            connectFlags.Write(packet.Username != null);

            stream.Write(connectFlags);
            stream.Write(packet.KeepAlivePeriod);
            stream.WriteWithLengthPrefix(packet.ClientId);

            if (packet.WillMessage != null)
            {
                stream.WriteWithLengthPrefix(packet.WillMessage.Topic);
                stream.WriteWithLengthPrefix(packet.WillMessage.Payload);
            }

            if (packet.Username != null)
            {
                stream.WriteWithLengthPrefix(packet.Username);
            }

            if (packet.Password != null)
            {
                stream.WriteWithLengthPrefix(packet.Password);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        private byte Serialize(MqttConnAckPacket packet, Stream stream)
        {
            if (ProtocolVersion == MqttProtocolVersion.V310)
            {
                stream.WriteByte(0);
            }
            else if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                var connectAcknowledgeFlags = new ByteWriter();
                connectAcknowledgeFlags.Write(packet.IsSessionPresent);

                stream.Write(connectAcknowledgeFlags);
            }
            else
            {
                throw new MqttProtocolViolationException("Protocol version not supported.");
            }

            stream.WriteByte((byte)packet.ConnectReturnCode);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static byte Serialize(MqttPubRelPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        private static byte Serialize(MqttPublishPacket packet, Stream stream)
        {
            ValidatePublishPacket(packet);

            stream.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (!packet.PacketIdentifier.HasValue)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                stream.Write(packet.PacketIdentifier.Value);
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
                stream.Write(packet.Payload, 0, packet.Payload.Length);
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

        private static byte Serialize(MqttPubAckPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte Serialize(MqttPubRecPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte Serialize(MqttPubCompPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte Serialize(MqttSubscribePacket packet, Stream stream)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Subscribe packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    stream.WriteWithLengthPrefix(topicFilter.Topic);
                    stream.WriteByte((byte)topicFilter.QualityOfServiceLevel);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        private static byte Serialize(MqttSubAckPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("SubAck packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            if (packet.SubscribeReturnCodes?.Any() == true)
            {
                foreach (var packetSubscribeReturnCode in packet.SubscribeReturnCodes)
                {
                    stream.WriteByte((byte)packetSubscribeReturnCode);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        private static byte Serialize(MqttUnsubscribePacket packet, Stream stream)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("Unsubscribe packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    stream.WriteWithLengthPrefix(topicFilter);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        private static byte Serialize(MqttUnsubAckPacket packet, Stream stream)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("UnsubAck packet has no packet identifier.");
            }

            stream.Write(packet.PacketIdentifier.Value);
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        private static byte SerializeEmptyPacket(MqttControlPacketType type)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }

        private static void ThrowIfBodyIsEmpty(Stream body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
