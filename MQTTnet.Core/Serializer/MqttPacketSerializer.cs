using System;
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

        public Task SerializeAsync(MqttBasePacket packet, IMqttCommunicationChannel destination)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            if (packet is MqttConnectPacket connectPacket)
            {
                return SerializeAsync(connectPacket, destination);
            }

            if (packet is MqttConnAckPacket connAckPacket)
            {
                return SerializeAsync(connAckPacket, destination);
            }

            if (packet is MqttDisconnectPacket disconnectPacket)
            {
                return SerializeAsync(disconnectPacket, destination);
            }

            if (packet is MqttPingReqPacket pingReqPacket)
            {
                return SerializeAsync(pingReqPacket, destination);
            }

            if (packet is MqttPingRespPacket pingRespPacket)
            {
                return SerializeAsync(pingRespPacket, destination);
            }

            if (packet is MqttPublishPacket publishPacket)
            {
                return SerializeAsync(publishPacket, destination);
            }

            if (packet is MqttPubAckPacket pubAckPacket)
            {
                return SerializeAsync(pubAckPacket, destination);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                return SerializeAsync(pubRecPacket, destination);
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return SerializeAsync(pubRelPacket, destination);
            }

            if (packet is MqttPubCompPacket pubCompPacket)
            {
                return SerializeAsync(pubCompPacket, destination);
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                return SerializeAsync(subscribePacket, destination);
            }

            if (packet is MqttSubAckPacket subAckPacket)
            {
                return SerializeAsync(subAckPacket, destination);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return SerializeAsync(unsubscribePacket, destination);
            }

            if (packet is MqttUnsubAckPacket unsubAckPacket)
            {
                return SerializeAsync(unsubAckPacket, destination);
            }

            throw new MqttProtocolViolationException("Packet type invalid.");
        }

        public async Task<MqttBasePacket> DeserializeAsync(IMqttCommunicationChannel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var mqttPacketReader = new MqttPacketReader(source))
            {
                await mqttPacketReader.ReadToEndAsync().ConfigureAwait(false);

                switch (mqttPacketReader.ControlPacketType)
                {
                    case MqttControlPacketType.Connect:
                        {
                            return await DeserializeConnectAsync(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.ConnAck:
                        {
                            return await DeserializeConnAck(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.Disconnect:
                        {
                            return new MqttDisconnectPacket();
                        }

                    case MqttControlPacketType.Publish:
                        {
                            return await DeserializePublishAsync(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.PubAck:
                        {
                            return new MqttPubAckPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
                            };
                        }

                    case MqttControlPacketType.PubRec:
                        {
                            return new MqttPubRecPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
                            };
                        }

                    case MqttControlPacketType.PubRel:
                        {
                            return new MqttPubRelPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
                            };
                        }

                    case MqttControlPacketType.PubComp:
                        {
                            return new MqttPubCompPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
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
                            return await DeserializeSubscribeAsync(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.SubAck:
                        {
                            return await DeserializeSubAck(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.Unsubscibe:
                        {
                            return await DeserializeUnsubscribeAsync(mqttPacketReader).ConfigureAwait(false);
                        }

                    case MqttControlPacketType.UnsubAck:
                        {
                            return new MqttUnsubAckPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
                            };
                        }

                    default:
                        {
                            throw new MqttProtocolViolationException($"Packet type ({(int)mqttPacketReader.ControlPacketType}) not supported.");
                        }
                }
            }
        }

        private static async Task<MqttBasePacket> DeserializeUnsubscribeAsync(MqttPacketReader reader)
        {
            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync().ConfigureAwait(false),
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false));
            }

            return packet;
        }

        private static async Task<MqttBasePacket> DeserializeSubscribeAsync(MqttPacketReader reader)
        {
            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync().ConfigureAwait(false),
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    await reader.ReadRemainingDataStringWithLengthPrefixAsync(),
                    (MqttQualityOfServiceLevel)await reader.ReadRemainingDataByteAsync().ConfigureAwait(false)));
            }

            return packet;
        }

        private static async Task<MqttBasePacket> DeserializePublishAsync(MqttPacketReader reader)
        {
            var fixedHeader = new ByteReader(reader.FixedHeader);
            var retain = fixedHeader.Read();
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)fixedHeader.Read(2);
            var dup = fixedHeader.Read();

            var topic = await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false);

            ushort packetIdentifier = 0;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = await reader.ReadRemainingDataUShortAsync().ConfigureAwait(false);
            }

            var packet = new MqttPublishPacket
            {
                Retain = retain,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup,
                Topic = topic,
                Payload = await reader.ReadRemainingDataAsync().ConfigureAwait(false),
                PacketIdentifier = packetIdentifier
            };

            return packet;
        }

        private static async Task<MqttBasePacket> DeserializeConnectAsync(MqttPacketReader reader)
        {
            await reader.ReadRemainingDataAsync(2).ConfigureAwait(false); // Skip 2 bytes

            MqttProtocolVersion protocolVersion;
            var protocolName = await reader.ReadRemainingDataAsync(4).ConfigureAwait(false);
            if (protocolName.SequenceEqual(ProtocolVersionV310Name))
            {
                await reader.ReadRemainingDataAsync(2).ConfigureAwait(false);
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

            var protocolLevel = await reader.ReadRemainingDataByteAsync().ConfigureAwait(false);
            var connectFlags = await reader.ReadRemainingDataByteAsync().ConfigureAwait(false);

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

            packet.KeepAlivePeriod = await reader.ReadRemainingDataUShortAsync().ConfigureAwait(false);
            packet.ClientId = await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false);

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage(
                    await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false),
                    await reader.ReadRemainingDataWithLengthPrefixAsync().ConfigureAwait(false),
                    (MqttQualityOfServiceLevel)willQoS,
                    willRetain);
            }

            if (usernameFlag)
            {
                packet.Username = await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false);
            }

            if (passwordFlag)
            {
                packet.Password = await reader.ReadRemainingDataStringWithLengthPrefixAsync().ConfigureAwait(false);
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private static async Task<MqttBasePacket> DeserializeSubAck(MqttPacketReader reader)
        {
            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync().ConfigureAwait(false)
            };

            while (!reader.EndOfRemainingData)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)await reader.ReadRemainingDataByteAsync().ConfigureAwait(false));
            }

            return packet;
        }

        private static async Task<MqttBasePacket> DeserializeConnAck(MqttPacketReader reader)
        {
            var variableHeader1 = await reader.ReadRemainingDataByteAsync().ConfigureAwait(false);
            var variableHeader2 = await reader.ReadRemainingDataByteAsync().ConfigureAwait(false);

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

        private Task SerializeAsync(MqttConnectPacket packet, IMqttCommunicationChannel destination)
        {
            ValidateConnectPacket(packet);

            using (var output = new MqttPacketWriter())
            {
                // Write variable header
                output.Write(0x00, 0x04); // 3.1.2.1 Protocol Name
                if (ProtocolVersion == MqttProtocolVersion.V311)
                {
                    output.Write(ProtocolVersionV311Name);
                    output.Write(0x04); // 3.1.2.2 Protocol Level (4)
                }
                else
                {
                    output.Write(ProtocolVersionV310Name);
                    output.Write(0x64);
                    output.Write(0x70);
                    output.Write(0x03); // Protocol Level (3)
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

                output.Write(connectFlags);
                output.Write(packet.KeepAlivePeriod);
                output.WriteWithLengthPrefix(packet.ClientId);

                if (packet.WillMessage != null)
                {
                    output.WriteWithLengthPrefix(packet.WillMessage.Topic);
                    output.WriteWithLengthPrefix(packet.WillMessage.Payload);
                }

                if (packet.Username != null)
                {
                    output.WriteWithLengthPrefix(packet.Username);
                }

                if (packet.Password != null)
                {
                    output.WriteWithLengthPrefix(packet.Password);
                }

                output.InjectFixedHeader(MqttControlPacketType.Connect);
                return output.WriteToAsync(destination);
            }
        }

        private Task SerializeAsync(MqttConnAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                var connectAcknowledgeFlags = new ByteWriter();

                if (ProtocolVersion == MqttProtocolVersion.V311)
                {
                    connectAcknowledgeFlags.Write(packet.IsSessionPresent);
                }
                
                output.Write(connectAcknowledgeFlags);
                output.Write((byte)packet.ConnectReturnCode);

                output.InjectFixedHeader(MqttControlPacketType.ConnAck);
                return output.WriteToAsync(destination);
            }
        }

        private static async Task SerializeAsync(MqttPubRelPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubRel, 0x02);
                await output.WriteToAsync(destination).ConfigureAwait(false);
            }
        }

        private static Task SerializeAsync(MqttDisconnectPacket packet, IMqttCommunicationChannel destination)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.Disconnect, destination);
        }

        private static Task SerializeAsync(MqttPingReqPacket packet, IMqttCommunicationChannel destination)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.PingReq, destination);
        }

        private static Task SerializeAsync(MqttPingRespPacket packet, IMqttCommunicationChannel destination)
        {
            return SerializeEmptyPacketAsync(MqttControlPacketType.PingResp, destination);
        }

        private static Task SerializeAsync(MqttPublishPacket packet, IMqttCommunicationChannel destination)
        {
            ValidatePublishPacket(packet);

            using (var output = new MqttPacketWriter())
            {
                output.WriteWithLengthPrefix(packet.Topic);

                if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    output.Write(packet.PacketIdentifier);
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
                    output.Write(packet.Payload);
                }

                var fixedHeader = new ByteWriter();
                fixedHeader.Write(packet.Retain);
                fixedHeader.Write((byte)packet.QualityOfServiceLevel, 2);
                fixedHeader.Write(packet.Dup);

                output.InjectFixedHeader(MqttControlPacketType.Publish, fixedHeader.Value);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttPubAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubAck);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttPubRecPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubRec);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttPubCompPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubComp);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttSubscribePacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                if (packet.TopicFilters?.Count > 0)
                {
                    foreach (var topicFilter in packet.TopicFilters)
                    {
                        output.WriteWithLengthPrefix(topicFilter.Topic);
                        output.Write((byte)topicFilter.QualityOfServiceLevel);
                    }
                }

                output.InjectFixedHeader(MqttControlPacketType.Subscribe, 0x02);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttSubAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                if (packet.SubscribeReturnCodes?.Any() == true)
                {
                    foreach (var packetSubscribeReturnCode in packet.SubscribeReturnCodes)
                    {
                        output.Write((byte)packetSubscribeReturnCode);
                    }
                }

                output.InjectFixedHeader(MqttControlPacketType.SubAck);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttUnsubscribePacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                if (packet.TopicFilters?.Any() == true)
                {
                    foreach (var topicFilter in packet.TopicFilters)
                    {
                        output.WriteWithLengthPrefix(topicFilter);
                    }
                }

                output.InjectFixedHeader(MqttControlPacketType.Unsubscibe, 0x02);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeAsync(MqttUnsubAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.UnsubAck);
                return output.WriteToAsync(destination);
            }
        }

        private static Task SerializeEmptyPacketAsync(MqttControlPacketType type, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.InjectFixedHeader(type);
                return output.WriteToAsync(destination);
            }
        }
    }
}
