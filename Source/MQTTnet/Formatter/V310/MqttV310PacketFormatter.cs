using System;
using System.Linq;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V310
{
    public class MqttV310PacketFormatter : IMqttPacketFormatter
    {
        private const int FixedHeaderSize = 1;

        private readonly MqttPacketWriter _packetWriter = new MqttPacketWriter();

        public ArraySegment<byte> Encode(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            // Leave enough head space for max header size (fixed + 4 variable remaining length = 5 bytes)
            _packetWriter.Reset();
            _packetWriter.Seek(5);

            var fixedHeader = SerializePacket(packet, _packetWriter);
            var remainingLength = (uint)(_packetWriter.Length - 5);

            var remainingLengthBuffer = MqttPacketWriter.EncodeVariableByteInteger(remainingLength);

            var headerSize = FixedHeaderSize + remainingLengthBuffer.Count;
            var headerOffset = 5 - headerSize;

            // Position cursor on correct offset on beginining of array (has leading 0x0)
            _packetWriter.Seek(headerOffset);
            _packetWriter.Write(fixedHeader);
            _packetWriter.Write(remainingLengthBuffer.Array, remainingLengthBuffer.Offset, remainingLengthBuffer.Count);

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
                case MqttControlPacketType.Connect: return DeserializeConnectPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.ConnAck: return DeserializeConnAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Disconnect: return new MqttDisconnectPacket();
                case MqttControlPacketType.Publish: return DeserializePublish(receivedMqttPacket);
                case MqttControlPacketType.PubAck: return DeserializePubAck(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRec: return DeserializePubRec(receivedMqttPacket.Body);
                case MqttControlPacketType.PubRel: return DeserializePubRel(receivedMqttPacket.Body);
                case MqttControlPacketType.PubComp: return DeserializePubComp(receivedMqttPacket.Body);
                case MqttControlPacketType.PingReq: return new MqttPingReqPacket();
                case MqttControlPacketType.PingResp: return new MqttPingRespPacket();
                case MqttControlPacketType.Subscribe: return DeserializeSubscribe(receivedMqttPacket.Body);
                case MqttControlPacketType.SubAck: return DeserializeSubAckPacket(receivedMqttPacket.Body);
                case MqttControlPacketType.Unsubscibe: return DeserializeUnsubscribe(receivedMqttPacket.Body);
                case MqttControlPacketType.UnsubAck: return DeserializeUnsubAck(receivedMqttPacket.Body);

                default: throw new MqttProtocolViolationException($"Packet type ({controlPacketType}) not supported.");
            }
        }

        public virtual MqttPublishPacket ConvertApplicationMessageToPublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage.UserProperties != null)
            {
                throw new MqttProtocolViolationException("User properties are not supported in MQTT version 3.");
            }

            return new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false
            };
        }

        public void FreeBuffer()
        {
            _packetWriter.FreeBuffer();
        }

        private byte SerializePacket(MqttBasePacket packet, MqttPacketWriter packetWriter)
        {
            switch (packet)
            {
                case MqttConnectPacket connectPacket: return SerializeConnectPacket(connectPacket, packetWriter);
                case MqttConnAckPacket connAckPacket: return SerializeConnAckPacket(connAckPacket, packetWriter);
                case MqttDisconnectPacket _: return SerializeEmptyPacket(MqttControlPacketType.Disconnect);
                case MqttPingReqPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingReq);
                case MqttPingRespPacket _: return SerializeEmptyPacket(MqttControlPacketType.PingResp);
                case MqttPublishPacket publishPacket: return SerializePublishPacket(publishPacket, packetWriter);
                case MqttPubAckPacket pubAckPacket: return SerializePubAckPacket(pubAckPacket, packetWriter);
                case MqttPubRecPacket pubRecPacket: return SerializePubRecPacket(pubRecPacket, packetWriter);
                case MqttPubRelPacket pubRelPacket: return SerializePubRelPacket(pubRelPacket, packetWriter);
                case MqttPubCompPacket pubCompPacket: return SerializePubCompPacket(pubCompPacket, packetWriter);
                case MqttSubscribePacket subscribePacket: return SerializeSubscribePacket(subscribePacket, packetWriter);
                case MqttSubAckPacket subAckPacket: return SerializeSubAckPacket(subAckPacket, packetWriter);
                case MqttUnsubscribePacket unsubscribePacket: return SerializeUnsubscribePacket(unsubscribePacket, packetWriter);
                case MqttUnsubAckPacket unsubAckPacket: return SerializeUnsubAckPacket(unsubAckPacket, packetWriter);
                default: throw new MqttProtocolViolationException("Packet type invalid.");
            }
        }

        private static MqttBasePacket DeserializeUnsubAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DeserializePubComp(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubCompPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DeserializePubRel(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRelPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DeserializePubRec(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubRecPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DeserializePubAck(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            return new MqttPubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };
        }

        private static MqttBasePacket DeserializeUnsubscribe(MqttPacketBodyReader body)
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

        private static MqttBasePacket DeserializeSubscribe(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
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
                packetIdentifier = receivedMqttPacket.Body.ReadTwoByteInteger();
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

        private MqttBasePacket DeserializeConnectPacket(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnectPacket
            {
                ProtocolName = body.ReadStringWithLengthPrefix(),
                ProtocolLevel = body.ReadByte()
            };
            
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

        private static MqttBasePacket DeserializeSubAckPacket(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = body.ReadTwoByteInteger()
            };

            while (!body.EndOfStream)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)body.ReadByte());
            }

            return packet;
        }

        protected virtual MqttBasePacket DeserializeConnAckPacket(MqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            body.ReadByte(); // Reserved.
            packet.ConnectReturnCode = (MqttConnectReturnCode)body.ReadByte();

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
        private static void ValidatePublishPacket(MqttPublishPacket packet)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }
        }

        protected virtual byte SerializeConnectPacket(MqttConnectPacket packet, MqttPacketWriter packetWriter)
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

        protected virtual byte SerializeConnAckPacket(MqttConnAckPacket packet, MqttPacketWriter packetWriter)
        {
            packetWriter.Write(0); // Reserved.
            packetWriter.Write((byte)packet.ConnectReturnCode);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static byte SerializePubRelPacket(MqttPubRelPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRel packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRel, 0x02);
        }

        protected virtual byte SerializePublishPacket(MqttPublishPacket packet, MqttPacketWriter packetWriter)
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

        protected virtual byte SerializePubAckPacket(MqttPubAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }

        private static byte SerializePubRecPacket(MqttPubRecPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubRec packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubRec);
        }

        private static byte SerializePubCompPacket(MqttPubCompPacket packet, MqttPacketWriter packetWriter)
        {
            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubComp packet has no packet identifier.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubComp);
        }

        private static byte SerializeSubscribePacket(MqttSubscribePacket packet, MqttPacketWriter packetWriter)
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

        private static byte SerializeSubAckPacket(MqttSubAckPacket packet, MqttPacketWriter packetWriter)
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

        private static byte SerializeUnsubscribePacket(MqttUnsubscribePacket packet, MqttPacketWriter packetWriter)
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

        private static byte SerializeUnsubAckPacket(MqttUnsubAckPacket packet, MqttPacketWriter packetWriter)
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
        protected static void ThrowIfBodyIsEmpty(MqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }
    }
}
