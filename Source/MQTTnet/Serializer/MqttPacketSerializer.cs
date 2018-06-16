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

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public byte[] Serialize(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var stream = MemoryBufferWriter.Get();
            
            try
            {
                var fixedHeader = SerializePacket(packet, stream);
                var bodyLength = (int)stream.Length;
                var headerLength = MqttPacketWriter.GetHeaderLength(bodyLength);
                
                var result = new byte[headerLength + bodyLength];
                result[0] = fixedHeader;
                MqttPacketWriter.WriteBodyLength(bodyLength, result);

                stream.CopyTo(result.AsSpan(headerLength));

                return result;
            }
            finally
            {
                MemoryBufferWriter.Return(stream);
            }
        }

        private byte SerializePacket(MqttBasePacket packet, MemoryBufferWriter packetWriter)
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
                case MqttControlPacketType.Connect: return DeserializeConnect(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.ConnAck: return DeserializeConnAck(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.Disconnect: return new MqttDisconnectPacket();
                case MqttControlPacketType.Publish: return DeserializePublish(receivedMqttPacket);
                case MqttControlPacketType.PubAck: return DeserializePubAck(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.PubRec: return DeserializePubRec(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.PubRel: return DeserializePubRel(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.PubComp: return DeserializePubComp(receivedMqttPacket.Body.Span);
                case MqttControlPacketType.PingReq: return new MqttPingReqPacket();
                case MqttControlPacketType.PingResp: return new MqttPingRespPacket();
                case MqttControlPacketType.Subscribe: return DeserializeSubscribe(receivedMqttPacket);
                case MqttControlPacketType.SubAck: return DeserializeSubAck(receivedMqttPacket);
                case MqttControlPacketType.Unsubscibe: return DeserializeUnsubscribe(receivedMqttPacket);
                case MqttControlPacketType.UnsubAck: return DeserializeUnsubAck(receivedMqttPacket.Body.Span);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        private static MqttBasePacket DeserializeUnsubAck(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            return new MqttUnsubAckPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubComp(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            return new MqttPubCompPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRel(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            return new MqttPubRelPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubRec(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            return new MqttPubRecPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializePubAck(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            return new MqttPubAckPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };
        }

        private static MqttBasePacket DeserializeUnsubscribe(ReceivedMqttPacket receivedMqttPacket)
        {
            ReadOnlySpan<byte> body = receivedMqttPacket.Body.Span;
            ThrowIfBodyIsEmpty(body);

            var remainingData = body;
            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = remainingData.ReadUInt16(),
            };

            while (remainingData.Length > 0)
            {
                packet.TopicFilters.Add(remainingData.ReadStringWithLengthPrefix());
            }

            return packet;
        }

        private static MqttBasePacket DeserializeSubscribe(ReceivedMqttPacket receivedMqttPacket)
        {
            ReadOnlySpan<byte> body = receivedMqttPacket.Body.Span;
            ThrowIfBodyIsEmpty(body);

            var remainingData = body;
            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };

            while (remainingData.Length > 0)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    remainingData.ReadStringWithLengthPrefix(),
                    (MqttQualityOfServiceLevel)remainingData.ReadByte()));
            }

            return packet;
        }

        private static MqttBasePacket DeserializeSubAck(ReceivedMqttPacket receivedMqttPacket)
        {
            ReadOnlySpan<byte> body = receivedMqttPacket.Body.Span;
            ThrowIfBodyIsEmpty(body);

            var remainingData = body;
            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = remainingData.ReadUInt16()
            };

            while (remainingData.Length > 0)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)remainingData.ReadByte());
            }

            return packet;
        }

        private static MqttBasePacket DeserializePublish(ReceivedMqttPacket receivedMqttPacket)
        {
            ReadOnlySpan<byte> body = receivedMqttPacket.Body.Span;
            ThrowIfBodyIsEmpty(body);

            var remainingData = body;
            var retain = (receivedMqttPacket.FixedHeader & 0x1) > 0;
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)(receivedMqttPacket.FixedHeader >> 1 & 0x3);
            var dup = (receivedMqttPacket.FixedHeader & 0x3) > 0;

            var topic = remainingData.ReadStringWithLengthPrefix();

            ushort? packetIdentifier = null;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = remainingData.ReadUInt16();
            }

            var packet = new MqttPublishPacket
            {
                PacketIdentifier = packetIdentifier,
                Retain = retain,
                Topic = topic,
                Payload = remainingData.ToArray(),
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup
            };

            return packet;
        }

        private static MqttBasePacket DeserializeConnect(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);
            var remainingData = input;

            var protocolName = remainingData.ReadStringWithLengthPrefix();

            MqttProtocolVersion protocolVersion;
            if (protocolName == "MQTT")
            {
                var protocolLevel = remainingData.ReadByte();
                if (protocolLevel != 4)
                {
                    throw new MqttProtocolViolationException($"Protocol level ({protocolLevel}) not supported for MQTT 3.1.1.");
                }

                protocolVersion = MqttProtocolVersion.V311;
            }
            else if (protocolName == "MQIsdp")
            {
                var protocolLevel = remainingData.ReadByte();
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

            var connectFlags = remainingData.ReadByte();
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

            packet.KeepAlivePeriod = remainingData.ReadUInt16();
            packet.ClientId = remainingData.ReadStringWithLengthPrefix();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage
                {
                    Topic = remainingData.ReadStringWithLengthPrefix(),
                    Payload = remainingData.ReadWithLengthPrefix(),
                    QualityOfServiceLevel = (MqttQualityOfServiceLevel)willQoS,
                    Retain = willRetain
                };
            }

            if (usernameFlag)
            {
                packet.Username = remainingData.ReadStringWithLengthPrefix();
            }

            if (passwordFlag)
            {
                packet.Password = remainingData.ReadStringWithLengthPrefix();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private MqttBasePacket DeserializeConnAck(in ReadOnlySpan<byte> input)
        {
            ThrowIfBodyIsEmpty(input);

            var remainingData = input;
            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = remainingData.ReadByte();
            
            if (ProtocolVersion == MqttProtocolVersion.V311)
            {
                packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            }

            packet.ConnectReturnCode = (MqttConnectReturnCode)remainingData.ReadByte();

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

        private byte Serialize(MqttConnectPacket packet, MemoryBufferWriter packetWriter)
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

        private byte Serialize(MqttConnAckPacket packet, MemoryBufferWriter packetWriter)
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

        private static byte Serialize(MqttPubRelPacket packet, MemoryBufferWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        private static byte Serialize(MqttPublishPacket packet, MemoryBufferWriter packetWriter)
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

        private static byte Serialize(MqttPubAckPacket packet, MemoryBufferWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte Serialize(MqttPubRecPacket packet, MemoryBufferWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte Serialize(MqttPubCompPacket packet, MemoryBufferWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte Serialize(MqttSubscribePacket packet, MemoryBufferWriter packetWriter)
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

        private static byte Serialize(MqttSubAckPacket packet, MemoryBufferWriter packetWriter)
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

        private static byte Serialize(MqttUnsubscribePacket packet, MemoryBufferWriter packetWriter)
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

        private static byte Serialize(MqttUnsubAckPacket packet, MemoryBufferWriter packetWriter)
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
        private static void ThrowIfBodyIsEmpty(in ReadOnlySpan<byte> input)
        {
            if (input == null || input.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
