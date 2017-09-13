using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
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
        private byte[] _readBuffer = new byte[BufferConstants.Size];  // TODO: What happens if the message is bigger?

        public async Task SerializeAsync(MqttBasePacket packet, IMqttCommunicationChannel destination)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using (var stream = new MemoryStream())
            using (var writer = new MqttPacketWriter(stream))
            {
                var header = new List<byte> { SerializePacket(packet, writer) };
                var body = stream.ToArray();
                MqttPacketWriter.BuildLengthHeader(body.Length, header);
                
                await destination.WriteAsync(header.ToArray()).ConfigureAwait(false);
                await destination.WriteAsync(body).ConfigureAwait(false);
            }
        }

        private byte SerializePacket(MqttBasePacket packet, MqttPacketWriter writer)
        {
            if (packet is MqttConnectPacket connectPacket)
            {
                return Serialize(connectPacket, writer);
            }

            if (packet is MqttConnAckPacket connAckPacket)
            {
                return Serialize(connAckPacket, writer);
            }

            if (packet is MqttDisconnectPacket disconnectPacket)
            {
                return Serialize(disconnectPacket, writer);
            }

            if (packet is MqttPingReqPacket pingReqPacket)
            {
                return Serialize(pingReqPacket, writer);
            }

            if (packet is MqttPingRespPacket pingRespPacket)
            {
                return Serialize(pingRespPacket, writer);
            }

            if (packet is MqttPublishPacket publishPacket)
            {
                return Serialize(publishPacket, writer);
            }

            if (packet is MqttPubAckPacket pubAckPacket)
            {
                return Serialize(pubAckPacket, writer);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                return Serialize(pubRecPacket, writer);
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return Serialize(pubRelPacket, writer);
            }

            if (packet is MqttPubCompPacket pubCompPacket)
            {
                return Serialize(pubCompPacket, writer);
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                return Serialize(subscribePacket, writer);
            }

            if (packet is MqttSubAckPacket subAckPacket)
            {
                return Serialize(subAckPacket, writer);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return Serialize(unsubscribePacket, writer);
            }

            if (packet is MqttUnsubAckPacket unsubAckPacket)
            {
                return Serialize(unsubAckPacket, writer);
            }

            throw new MqttProtocolViolationException("Packet type invalid.");
        }

        public async Task<MqttBasePacket> DeserializeAsync(IMqttCommunicationChannel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var header = await MqttPacketReader.ReadHeaderFromSourceAsync(source, _readBuffer).ConfigureAwait(false);
            var body = await GetBody(source, header).ConfigureAwait(false);

            using (var mqttPacketReader = new MqttPacketReader(header, body))
            {
                switch (header.ControlPacketType)
                {
                    case MqttControlPacketType.Connect:
                        {
                            return DeserializeConnect(mqttPacketReader);
                        }

                    case MqttControlPacketType.ConnAck:
                        {
                            return DeserializeConnAck(mqttPacketReader);
                        }

                    case MqttControlPacketType.Disconnect:
                        {
                            return new MqttDisconnectPacket();
                        }

                    case MqttControlPacketType.Publish:
                        {
                            return DeserializePublish(mqttPacketReader, header);
                        }

                    case MqttControlPacketType.PubAck:
                        {
                            return new MqttPubAckPacket
                            {
                                PacketIdentifier = mqttPacketReader.ReadUInt16()
                            };
                        }

                    case MqttControlPacketType.PubRec:
                        {
                            return new MqttPubRecPacket
                            {
                                PacketIdentifier = mqttPacketReader.ReadUInt16()
                            };
                        }

                    case MqttControlPacketType.PubRel:
                        {
                            return new MqttPubRelPacket
                            {
                                PacketIdentifier = mqttPacketReader.ReadUInt16()
                            };
                        }

                    case MqttControlPacketType.PubComp:
                        {
                            return new MqttPubCompPacket
                            {
                                PacketIdentifier = mqttPacketReader.ReadUInt16()
                            };
                        }

                    case MqttControlPacketType.PingReq:
                        {
                            return new MqttPingReqPacket();
                        }

                    case MqttControlPacketType.PingResp:
                        {
                            return new MqttPingRespPacket();
                        }

                    case MqttControlPacketType.Subscribe:
                        {
                            return DeserializeSubscribe(mqttPacketReader);
                        }

                    case MqttControlPacketType.SubAck:
                        {
                            return DeserializeSubAck(mqttPacketReader);
                        }

                    case MqttControlPacketType.Unsubscibe:
                        {
                            return DeserializeUnsubscribe(mqttPacketReader);
                        }

                    case MqttControlPacketType.UnsubAck:
                        {
                            return new MqttUnsubAckPacket
                            {
                                PacketIdentifier = mqttPacketReader.ReadUInt16()
                            };
                        }

                    default:
                        {
                            throw new MqttProtocolViolationException($"Packet type ({(int)header.ControlPacketType}) not supported.");
                        }
                }
            }
        }

        private async Task<MemoryStream> GetBody(IMqttCommunicationChannel source, MqttPacketHeader header)
        {
            if (header.BodyLength > 0)
            {
                var segment = await MqttPacketReader.ReadFromSourceAsync(source, header.BodyLength, _readBuffer).ConfigureAwait(false);
                return new MemoryStream(segment.Array, segment.Offset, segment.Count);
            }
           
            return new MemoryStream();
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

            var protocolLevel = reader.ReadByte();
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
                packet.WillMessage = new MqttApplicationMessage(
                    reader.ReadStringWithLengthPrefix(),
                    reader.ReadWithLengthPrefix(),
                    (MqttQualityOfServiceLevel)willQoS,
                    willRetain);
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
            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }
        }

        private static void ValidatePublishPacket(MqttPublishPacket packet)
        {
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

        private static byte Serialize(MqttDisconnectPacket packet, MqttPacketWriter writer)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.Disconnect, writer);
        }

        private static byte Serialize(MqttPingReqPacket packet, MqttPacketWriter writer)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.PingReq, writer);
        }

        private static byte Serialize(MqttPingRespPacket packet, MqttPacketWriter writer)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.PingResp, writer);
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

            var fixedHeader = new ByteWriter();
            fixedHeader.Write(packet.Retain);
            fixedHeader.Write((byte)packet.QualityOfServiceLevel, 2);
            fixedHeader.Write(packet.Dup);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Publish, fixedHeader.Value);
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

        private static byte Serialize(MqttUnsubAckPacket packet, MqttPacketWriter writer)
        {
            writer.Write(packet.PacketIdentifier);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.UnsubAck);
        }

        private static byte SerializeEmptyPacketAsync(MqttControlPacketType type, MqttPacketWriter writer)
        {
            return MqttPacketWriter.BuildFixedHeader(type);
        }
    }
}
