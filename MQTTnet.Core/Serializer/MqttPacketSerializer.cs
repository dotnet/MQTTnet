using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketSerializer : IMqttPacketSerializer
    {
        private static byte[] ProtocolVersionV311Name { get; } = Encoding.UTF8.GetBytes("MQTT");
        private static byte[] ProtocolVersionV310Name { get; } = Encoding.UTF8.GetBytes("MQIs");

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public byte[] Serialize(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            using (var stream = new MemoryStream())
            using (var writer = new MqttPacketWriter(stream))
            {
                var fixedHeader = SerializePacket(packet, writer);
                var headerBuffer = new List<byte> { fixedHeader };
                MqttPacketWriter.WriteRemainingLength((int)stream.Length, headerBuffer);

                var header = headerBuffer.ToArray();
                var body = stream.ToArray();

                var buffer = new byte[header.Length + body.Length];
                Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
                Buffer.BlockCopy(body, 0, buffer, header.Length, body.Length);

                return buffer;
            }
        }

        public MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket)
        {
            if (receivedMqttPacket == null) throw new ArgumentNullException(nameof(receivedMqttPacket));

            using (var reader = new MqttPacketReader(receivedMqttPacket))
            {
                return Deserialize(receivedMqttPacket.Header, reader);
            }
        }

        private byte SerializePacket(MqttBasePacket packet, MqttPacketWriter writer)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return Serialize(connectPacket, writer);
                case MqttConnAckPacket connAckPacket: return Serialize(connAckPacket, writer);
                case MqttDisconnectPacket _: return SerializeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return Serialize(publishPacket, writer);
                case MqttPubAckPacket pubAckPacket: return Serialize(pubAckPacket, writer);
                case MqttPubRecPacket pubRecPacket: return Serialize(pubRecPacket, writer);
                case MqttPubRelPacket pubRelPacket: return Serialize(pubRelPacket, writer);
                case MqttPubCompPacket pubCompPacket: return Serialize(pubCompPacket, writer);
                case MqttSubscribePacket subscribePacket: return Serialize(subscribePacket, writer);
                case MqttSubAckPacket subAckPacket: return Serialize(subAckPacket, writer);
                case MqttUnsubscribePacket unsubscribePacket: return Serialize(unsubscribePacket, writer);
                case MqttUnsubAckPacket unsubAckPacket: return Serialize(unsubAckPacket, writer);
                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        private static MqttBasePacket Deserialize(MqttPacketHeader header, MqttPacketReader reader)
        {
            switch (header.ControlPacketType)
            {
                case MqttControlPacketType.Connect: return DeserializeConnect(reader);
                case MqttControlPacketType.ConnAck: return DeserializeConnAck(reader);
                case MqttControlPacketType.Disconnect: return new MqttDisconnectPacket();
                case MqttControlPacketType.Publish: return DeserializePublish(reader, header);
                case MqttControlPacketType.PubAck: return DeserializePubAck(reader);
                case MqttControlPacketType.PubRec: return DeserializePubRec(reader);
                case MqttControlPacketType.PubRel: return DeserializePubRel(reader);
                case MqttControlPacketType.PubComp: return DeserializePubComp(reader);
                case MqttControlPacketType.PingReq: return new MqttPingReqPacket();
                case MqttControlPacketType.PingResp: return new MqttPingRespPacket();
                case MqttControlPacketType.Subscribe: return DeserializeSubscribe(reader);
                case MqttControlPacketType.SubAck: return DeserializeSubAck(reader);
                case MqttControlPacketType.Unsubscibe: return DeserializeUnsubscribe(reader);
                case MqttControlPacketType.UnsubAck: return DeserializeUnsubAck(reader);
                default: throw new MqttProtocolViolationException($"Packet type ({(int)header.ControlPacketType}) not supported.");
            }
        }

        private static MqttBasePacket DeserializeUnsubAck(MqttPacketReader reader)
        {
            return new MqttUnsubAckPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubComp(MqttPacketReader reader)
        {
            return new MqttPubCompPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRel(MqttPacketReader reader)
        {
            return new MqttPubRelPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRec(MqttPacketReader reader)
        {
            return new MqttPubRecPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubAck(MqttPacketReader reader)
        {
            return new MqttPubAckPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializeUnsubscribe(MqttPacketReader reader)
        {
            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = reader.ReadUInt16(),
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(reader.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DeserializeSubscribe(MqttPacketReader reader)
        {
            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    reader.ReadStringWithLengthPrefix(),
                    (MqttQualityOfServiceLevel)reader.ReadByte()));
            }

            return packet;
        }

        private static MqttBasePacket DeserializePublish(MqttPacketReader reader, MqttPacketHeader mqttPacketHeader)
        {
            var fixedHeader = new ByteReader(mqttPacketHeader.FixedHeader);
            var retain = fixedHeader.Read();
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)fixedHeader.Read(2);
            var dup = fixedHeader.Read();

            var topic = reader.ReadStringWithLengthPrefix();

            ushort packetIdentifier = 0;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = reader.ReadUInt16();
            }

            var packet = new MqttPublishPacket
            {
                Retain = retain,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup,
                Topic = topic,
                Payload = reader.ReadRemainingData(),
                PacketIdentifier = packetIdentifier
            };

            return packet;
        }

        private static MqttBasePacket DeserializeConnect(MqttPacketReader reader)
        {
            reader.ReadBytes(2); // Skip 2 bytes

            MqttProtocolVersion protocolVersion;
            var protocolName = reader.ReadBytes(4);
            if (protocolName.SequenceEqual(ProtocolVersionV310Name))
            {
                reader.ReadBytes(2);
                protocolVersion = MqttProtocolVersion.V310;
            }
            else if (protocolName.SequenceEqual(ProtocolVersionV311Name))
            {
                protocolVersion = MqttProtocolVersion.V311;
            }
            else
            {
                throw new MqttProtocolViolationException("Protocol name is not supported.");
            }

            reader.ReadByte(); // Skip protocol level
            var connectFlags = reader.ReadByte();

            var connectFlagsReader = new ByteReader(connectFlags);
            connectFlagsReader.Read(); // Reserved.

            var packet = new MqttConnectPacket
            {
                ProtocolVersion = protocolVersion,
                CleanSession = connectFlagsReader.Read()
            };

            var willFlag = connectFlagsReader.Read();
            var willQoS = connectFlagsReader.Read(2);
            var willRetain = connectFlagsReader.Read();
            var passwordFlag = connectFlagsReader.Read();
            var usernameFlag = connectFlagsReader.Read();

            packet.KeepAlivePeriod = reader.ReadUInt16();
            packet.ClientId = reader.ReadStringWithLengthPrefix();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage
                {
                    Topic = reader.ReadStringWithLengthPrefix(),
                    Payload = reader.ReadWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)willQoS,
                    Retain = willRetain
                };
            }

            if (usernameFlag)
            {
                packet.Username = reader.ReadStringWithLengthPrefix();
            }

            if (passwordFlag)
            {
                packet.Password = reader.ReadStringWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private static MqttBasePacket DeserializeSubAck(MqttPacketReader reader)
        {
            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = reader.ReadUInt16()
            };

            while (!reader.EndOfRemainingData)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)reader.ReadByte());
            }

            return packet;
        }

        private static MqttBasePacket DeserializeConnAck(MqttPacketReader reader)
        {
            var variableHeader1 = reader.ReadByte();
            var variableHeader2 = reader.ReadByte();

            var packet = new MqttConnAckPacket
            {
                IsSessionPresent = new ByteReader(variableHeader1).Read(),
                ConnectReturnCode = (MqttConnectReturnCode)variableHeader2
            };

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

        private byte Serialize(MqttConnectPacket packet, MqttPacketWriter writer)
        {
            ValidateConnectPacket(packet);

            // Write variable header
            writer.Write(0x00, 0x04); // 3.1.2.1 Protocol Name
            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                writer.Write(ProtocolVersionV311Name);
                writer.Write(0x04); // 3.1.2.2 Protocol Level (4)
            }
            else
            {
                writer.Write(ProtocolVersionV310Name);
                writer.Write(0x64, 0x70, 0x03); // Protocol Level (0x03)
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

            connectFlags.Write(packet.Password != null);
            connectFlags.Write(packet.Username != null);

            writer.Write(connectFlags);
            writer.Write(packet.KeepAlivePeriod);
            writer.WriteWithLengthPrefix(packet.ClientId);

            if (packet.WillMessage != null)
            {
                writer.WriteWithLengthPrefix(packet.WillMessage.Topic);
                writer.WriteWithLengthPrefix(packet.WillMessage.Payload);
            }

            if (packet.Username != null)
            {
                writer.WriteWithLengthPrefix(packet.Username);
            }

            if (packet.Password != null)
            {
                writer.WriteWithLengthPrefix(packet.Password);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        private byte Serialize(MqttConnAckPacket packet, MqttPacketWriter writer)
        {
            var connectAcknowledgeFlags = new ByteWriter();

            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                connectAcknowledgeFlags.Write(packet.IsSessionPresent);
            }

            writer.Write(connectAcknowledgeFlags);
            writer.Write((byte)packet.ConnectReturnCode);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static byte Serialize(MqttPubRelPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        private static byte Serialize(MqttPublishPacket packet, MqttPacketWriter writer)
        {
            ValidatePublishPacket(packet);

            writer.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                writer.Write(packet.PacketIdentifier);
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
                writer.Write(packet.Payload);
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

        private static byte Serialize(MqttPubAckPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte Serialize(MqttPubRecPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte Serialize(MqttPubCompPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte Serialize(MqttSubscribePacket packet, MqttPacketWriter writer)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            writer.Write(packet.PacketIdentifier);

            if (packet.TopicFilters?.Count > 0)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    writer.WriteWithLengthPrefix(topicFilter.Topic);
                    writer.Write((byte)topicFilter.QualityOfServiceLevel);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Subscribe, 0x02);
        }

        private static byte Serialize(MqttSubAckPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            if (packet.SubscribeReturnCodes?.Any() == true)
            {
                foreach (var packetSubscribeReturnCode in packet.SubscribeReturnCodes)
                {
                    writer.Write((byte)packetSubscribeReturnCode);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.SubAck);
        }

        private static byte Serialize(MqttUnsubscribePacket packet, MqttPacketWriter writer)
        {
            if (!packet.TopicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");

            writer.Write(packet.PacketIdentifier);

            if (packet.TopicFilters?.Any() == true)
            {
                foreach (var topicFilter in packet.TopicFilters)
                {
                    writer.WriteWithLengthPrefix(topicFilter);
                }
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
        }

        private static byte Serialize(IMqttPacketWithIdentifier packet, BinaryWriter writer)
        {
            writer.Write(packet.PacketIdentifier);
            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        private static byte SerializeEmptyPacket(MqttControlPacketType type)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }
    }
}
