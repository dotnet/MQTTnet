using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Serializer
{
    public class DefaultMqttV311PacketSerializer : IMqttPacketSerializer
    {
        public async Task SerializeAsync(MqttBasePacket packet, IMqttCommunicationChannel destination)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            var connectPacket = packet as MqttConnectPacket;
            if (connectPacket != null)
            {
                await SerializeAsync(connectPacket, destination);
                return;
            }

            var connAckPacket = packet as MqttConnAckPacket;
            if (connAckPacket != null)
            {
                await SerializeAsync(connAckPacket, destination);
                return;
            }

            var disconnectPacket = packet as MqttDisconnectPacket;
            if (disconnectPacket != null)
            {
                await SerializeAsync(disconnectPacket, destination);
                return;
            }

            var pingReqPacket = packet as MqttPingReqPacket;
            if (pingReqPacket != null)
            {
                await SerializeAsync(pingReqPacket, destination);
                return;
            }

            var pingRespPacket = packet as MqttPingRespPacket;
            if (pingRespPacket != null)
            {
                await SerializeAsync(pingRespPacket, destination);
                return;
            }

            var publishPacket = packet as MqttPublishPacket;
            if (publishPacket != null)
            {
                await SerializeAsync(publishPacket, destination);
                return;
            }

            var pubAckPacket = packet as MqttPubAckPacket;
            if (pubAckPacket != null)
            {
                await SerializeAsync(pubAckPacket, destination);
                return;
            }

            var pubRecPacket = packet as MqttPubRecPacket;
            if (pubRecPacket != null)
            {
                await SerializeAsync(pubRecPacket, destination);
                return;
            }

            var pubRelPacket = packet as MqttPubRelPacket;
            if (pubRelPacket != null)
            {
                await SerializeAsync(pubRelPacket, destination);
                return;
            }

            var pubCompPacket = packet as MqttPubCompPacket;
            if (pubCompPacket != null)
            {
                await SerializeAsync(pubCompPacket, destination);
                return;
            }

            var subscribePacket = packet as MqttSubscribePacket;
            if (subscribePacket != null)
            {
                await SerializeAsync(subscribePacket, destination);
                return;
            }

            var subAckPacket = packet as MqttSubAckPacket;
            if (subAckPacket != null)
            {
                await SerializeAsync(subAckPacket, destination);
                return;
            }

            var unsubscribePacket = packet as MqttUnsubscribePacket;
            if (unsubscribePacket != null)
            {
                await SerializeAsync(unsubscribePacket, destination);
                return;
            }

            var unsubAckPacket = packet as MqttUnsubAckPacket;
            if (unsubAckPacket != null)
            {
                await SerializeAsync(unsubAckPacket, destination);
                return;
            }

            throw new MqttProtocolViolationException("Packet type invalid.");
        }

        public async Task<MqttBasePacket> DeserializeAsync(IMqttCommunicationChannel source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var mqttPacketReader = new MqttPacketReader(source))
            {
                await mqttPacketReader.ReadToEndAsync();

                switch (mqttPacketReader.ControlPacketType)
                {
                    case MqttControlPacketType.Connect:
                        {
                            return await DeserializeConnectAsync(mqttPacketReader);
                        }

                    case MqttControlPacketType.ConnAck:
                        {
                            return await DeserializeConnAck(mqttPacketReader);
                        }

                    case MqttControlPacketType.Disconnect:
                        {
                            return new MqttDisconnectPacket();
                        }

                    case MqttControlPacketType.Publish:
                        {
                            return await DeserializePublishAsync(mqttPacketReader);
                        }

                    case MqttControlPacketType.PubAck:
                        {
                            return new MqttPubAckPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync()
                            };
                        }

                    case MqttControlPacketType.PubRec:
                        {
                            return new MqttPubRecPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync()
                            };
                        }

                    case MqttControlPacketType.PubRel:
                        {
                            return new MqttPubRelPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync()
                            };
                        }

                    case MqttControlPacketType.PubComp:
                        {
                            return new MqttPubCompPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync()
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
                            return await DeserializeSubscribeAsync(mqttPacketReader);
                        }

                    case MqttControlPacketType.SubAck:
                        {
                            return await DeserializeSubAck(mqttPacketReader);
                        }

                    case MqttControlPacketType.Unsubscibe:
                        {
                            return await DeserializeUnsubscribeAsync(mqttPacketReader);
                        }

                    case MqttControlPacketType.UnsubAck:
                        {
                            return new MqttUnsubAckPacket
                            {
                                PacketIdentifier = await mqttPacketReader.ReadRemainingDataUShortAsync()
                            };
                        }

                    default:
                        {
                            throw new ProtocolViolationException();
                        }
                }
            }
        }

        private async Task<MqttBasePacket> DeserializeUnsubscribeAsync(MqttPacketReader reader)
        {
            var packet = new MqttUnsubscribePacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync(),
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(await reader.ReadRemainingDataStringWithLengthPrefixAsync());
            }

            return packet;
        }

        private async Task<MqttBasePacket> DeserializeSubscribeAsync(MqttPacketReader reader)
        {
            var packet = new MqttSubscribePacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync(),
            };

            while (!reader.EndOfRemainingData)
            {
                packet.TopicFilters.Add(new TopicFilter(
                    await reader.ReadRemainingDataStringWithLengthPrefixAsync(),
                    (MqttQualityOfServiceLevel)await reader.ReadRemainingDataByteAsync()));
            }

            return packet;
        }

        private async Task<MqttBasePacket> DeserializePublishAsync(MqttPacketReader reader)
        {
            var fixedHeader = new ByteReader(reader.FixedHeader);
            var retain = fixedHeader.Read();
            var qualityOfServiceLevel = (MqttQualityOfServiceLevel)fixedHeader.Read(2);
            var dup = fixedHeader.Read();

            var topic = await reader.ReadRemainingDataStringWithLengthPrefixAsync();

            ushort packetIdentifier = 0;
            if (qualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                packetIdentifier = await reader.ReadRemainingDataUShortAsync();
            }

            var packet = new MqttPublishPacket
            {
                Retain = retain,
                QualityOfServiceLevel = qualityOfServiceLevel,
                Dup = dup,
                Topic = topic,
                Payload = await reader.ReadRemainingDataAsync(),
                PacketIdentifier = packetIdentifier
            };

            return packet;
        }

        private async Task<MqttBasePacket> DeserializeConnectAsync(MqttPacketReader reader)
        {
            var packet = new MqttConnectPacket();

            await reader.ReadRemainingDataByteAsync();
            await reader.ReadRemainingDataByteAsync();
            var protocolName = await reader.ReadRemainingDataAsync(4);

            if (Encoding.UTF8.GetString(protocolName, 0, protocolName.Length) != "MQTT")
            {
                throw new ProtocolViolationException("Protocol name is not 'MQTT'.");
            }

            var protocolLevel = await reader.ReadRemainingDataByteAsync();
            var connectFlags = await reader.ReadRemainingDataByteAsync();

            var connectFlagsReader = new ByteReader(connectFlags);
            connectFlagsReader.Read(); // Reserved.
            packet.CleanSession = connectFlagsReader.Read();
            var willFlag = connectFlagsReader.Read();
            var willQoS = connectFlagsReader.Read(2);
            var willRetain = connectFlagsReader.Read();
            var passwordFlag = connectFlagsReader.Read();
            var usernameFlag = connectFlagsReader.Read();

            packet.KeepAlivePeriod = await reader.ReadRemainingDataUShortAsync();
            packet.ClientId = await reader.ReadRemainingDataStringWithLengthPrefixAsync();

            if (willFlag)
            {
                packet.WillMessage = new MqttApplicationMessage(
                    await reader.ReadRemainingDataStringWithLengthPrefixAsync(),
                    await reader.ReadRemainingDataWithLengthPrefixAsync(),
                    (MqttQualityOfServiceLevel)willQoS,
                    willRetain);
            }

            if (usernameFlag)
            {
                packet.Username = await reader.ReadRemainingDataStringWithLengthPrefixAsync();
            }

            if (passwordFlag)
            {
                packet.Password = await reader.ReadRemainingDataStringWithLengthPrefixAsync();
            }

            ValidateConnectPacket(packet);
            return packet;
        }

        private async Task<MqttBasePacket> DeserializeSubAck(MqttPacketReader reader)
        {
            var packet = new MqttSubAckPacket
            {
                PacketIdentifier = await reader.ReadRemainingDataUShortAsync()
            };

            while (!reader.EndOfRemainingData)
            {
                packet.SubscribeReturnCodes.Add((MqttSubscribeReturnCode)await reader.ReadRemainingDataByteAsync());
            }

            return packet;
        }

        private async Task<MqttBasePacket> DeserializeConnAck(MqttPacketReader reader)
        {
            var variableHeader1 = await reader.ReadRemainingDataByteAsync();
            var variableHeader2 = await reader.ReadRemainingDataByteAsync();

            var packet = new MqttConnAckPacket
            {
                IsSessionPresent = new ByteReader(variableHeader1).Read(),
                ConnectReturnCode = (MqttConnectReturnCode)variableHeader2
            };

            return packet;
        }

        private void ValidateConnectPacket(MqttConnectPacket packet)
        {
            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new ProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }

            if (!string.IsNullOrEmpty(packet.ClientId) && !Regex.IsMatch(packet.ClientId, "^[a-zA-Z0-9]*$"))
            {
                throw new ProtocolViolationException("ClientId contains invalid characters [MQTT-3.1.3-5].");
            }
        }

        private void ValidatePublishPacket(MqttPublishPacket packet)
        {
            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new ProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }
        }

        private async Task SerializeAsync(MqttConnectPacket packet, IMqttCommunicationChannel destination)
        {
            ValidateConnectPacket(packet);

            using (var output = new MqttPacketWriter())
            {
                // Write variable header
                output.Write(0x00); // 3.1.2.1 Protocol Name
                output.Write(0x04); // ""
                output.Write('M');
                output.Write('Q');
                output.Write('T');
                output.Write('T');
                output.Write(0x04); // 3.1.2.2 Protocol Level

                var connectFlags = new ByteWriter(); // 3.1.2.3 Connect Flags
                connectFlags.Write(false); // Reserved
                connectFlags.Write(packet.CleanSession);
                connectFlags.Write(packet.WillMessage != null);

                if (packet.WillMessage != null)
                {
                    connectFlags.Write((byte)packet.WillMessage.QualityOfServiceLevel, 2);
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
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttConnAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                var connectAcknowledgeFlags = new ByteWriter();
                connectAcknowledgeFlags.Write(packet.IsSessionPresent);

                output.Write(connectAcknowledgeFlags);
                output.Write((byte)packet.ConnectReturnCode);

                output.InjectFixedHeader(MqttControlPacketType.ConnAck);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttDisconnectPacket packet, IMqttCommunicationChannel destination)
        {
            await SerializeEmptyPacketAsync(MqttControlPacketType.Disconnect, destination);
        }

        private async Task SerializeAsync(MqttPingReqPacket packet, IMqttCommunicationChannel destination)
        {
            await SerializeEmptyPacketAsync(MqttControlPacketType.PingReq, destination);
        }

        private async Task SerializeAsync(MqttPingRespPacket packet, IMqttCommunicationChannel destination)
        {
            await SerializeEmptyPacketAsync(MqttControlPacketType.PingResp, destination);
        }

        private async Task SerializeAsync(MqttPublishPacket packet, IMqttCommunicationChannel destination)
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
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttPubAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubAck);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttPubRecPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubRec);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttPubRelPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubRel, 0x02);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttPubCompPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.PubComp);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttSubscribePacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                if (packet.TopicFilters?.Any() == true)
                {
                    foreach (var topicFilter in packet.TopicFilters)
                    {
                        output.WriteWithLengthPrefix(topicFilter.Topic);
                        output.Write((byte)topicFilter.QualityOfServiceLevel);
                    }
                }

                output.InjectFixedHeader(MqttControlPacketType.Subscribe, 0x02);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttSubAckPacket packet, IMqttCommunicationChannel destination)
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
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttUnsubscribePacket packet, IMqttCommunicationChannel destination)
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
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeAsync(MqttUnsubAckPacket packet, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.Write(packet.PacketIdentifier);

                output.InjectFixedHeader(MqttControlPacketType.UnsubAck);
                await output.WriteToAsync(destination);
            }
        }

        private async Task SerializeEmptyPacketAsync(MqttControlPacketType type, IMqttCommunicationChannel destination)
        {
            using (var output = new MqttPacketWriter())
            {
                output.InjectFixedHeader(type);
                await output.WriteToAsync(destination);
            }
        }
    }
}
