// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.Helpers;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.Formatter
{
    [TestClass]
    public sealed class MqttPacketSerialization_V3_Binary_Tests
    {
        [TestMethod]
        public void DeserializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            DeserializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
        }

        [TestMethod]
        public void DeserializeV311_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                IsSessionPresent = true,
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            DeserializeAndCompare(p, "IAIBBQ==");
        }

        [TestMethod]
        public void DeserializeV311_MqttConnectPacket()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true
            };

            DeserializeAndCompare(p, "EBsABE1RVFQEwgB7AANYWVoABFVTRVIABFBBU1M=");
        }

        [TestMethod]
        public void DeserializeV311_MqttConnectPacketWithWillMessage()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true,
                WillFlag = true,
                WillTopic = "My/last/will",
                WillMessage = Encoding.UTF8.GetBytes("Good byte."),
                WillQoS = MqttQualityOfServiceLevel.AtLeastOnce,
                WillRetain = true
            };

            DeserializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
        }

        [TestMethod]
        public void DeserializeV311_MqttPubAckPacket()
        {
            var p = new MqttPubAckPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "QAIAew==");
        }

        [TestMethod]
        public void DeserializeV311_MqttPubCompPacket()
        {
            var p = new MqttPubCompPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "cAIAew==");
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket()
        {
            var p = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                PayloadSegment = Encoding.ASCII.GetBytes("HELLO"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "A/B/C"
            };

            DeserializeAndCompare(p, "Ow4ABUEvQi9DAHtIRUxMTw==");
        }


        [TestMethod]
        public void DeserializeV311_MqttPublishPacket_DupFalse()
        {
            var p = new MqttPublishPacket
            {
                Dup = false
            };

            var p2 = Roundtrip(p);

            Assert.AreEqual(p.Dup, p2.Dup);
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket_Qos1()
        {
            var p = new MqttPublishPacket
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };

            var p2 = Roundtrip(p);

            Assert.AreEqual(p.QualityOfServiceLevel, p2.QualityOfServiceLevel);
            Assert.AreEqual(p.Dup, p2.Dup);
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket_Qos2()
        {
            var p = new MqttPublishPacket
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                PacketIdentifier = 1
            };

            var p2 = Roundtrip(p);

            Assert.AreEqual(p.QualityOfServiceLevel, p2.QualityOfServiceLevel);
            Assert.AreEqual(p.Dup, p2.Dup);
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket_Qos3()
        {
            var p = new MqttPublishPacket
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                PacketIdentifier = 1
            };

            var p2 = Roundtrip(p);

            Assert.AreEqual(p.QualityOfServiceLevel, p2.QualityOfServiceLevel);
            Assert.AreEqual(p.Dup, p2.Dup);
        }

        [TestMethod]
        public void DeserializeV311_MqttPubRecPacket()
        {
            var p = new MqttPubRecPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "UAIAew==");
        }

        [TestMethod]
        public void DeserializeV311_MqttPubRelPacket()
        {
            var p = new MqttPubRelPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "YgIAew==");
        }

        [TestMethod]
        public void DeserializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123,
                ReasonCodes = new List<MqttSubscribeReasonCode>
                {
                    MqttSubscribeReasonCode.GrantedQoS0,
                    MqttSubscribeReasonCode.GrantedQoS1,
                    MqttSubscribeReasonCode.GrantedQoS2,
                    MqttSubscribeReasonCode.UnspecifiedError
                }
            };

            DeserializeAndCompare(p, "kAYAewABAoA=");
        }

        [TestMethod]
        public void DeserializeV311_MqttSubscribePacket()
        {
            var p = new MqttSubscribePacket
            {
                PacketIdentifier = 123,
                TopicFilters = new List<MqttTopicFilter>
                {
                    new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce },
                    new MqttTopicFilter { Topic = "1/2/3", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce },
                    new MqttTopicFilter { Topic = "x/y/z", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce }
                }
            };

            DeserializeAndCompare(p, "ghoAewAFQS9CL0MCAAUxLzIvMwEABXgveS96AA==");
        }

        [TestMethod]
        public void DeserializeV311_MqttUnsubAckPacket()
        {
            var p = new MqttUnsubAckPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "sAIAew==");
        }

        [TestMethod]
        public void DeserializeV311_MqttUnsubscribePacket()
        {
            var p = new MqttUnsubscribePacket
            {
                PacketIdentifier = 123
            };

            p.TopicFilters.Add("A/B/C");
            p.TopicFilters.Add("1/2/3");
            p.TopicFilters.Add("x/y/z");

            DeserializeAndCompare(p, "ohcAewAFQS9CL0MABTEvMi8zAAV4L3kveg==");
        }

        [TestMethod]
        public void DetectVersionFromMqttConnectPacket()
        {
            var packet = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true
            };

            Assert.AreEqual(
                MqttProtocolVersion.V310,
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535)), Serialize(packet, MqttProtocolVersion.V310)));

            Assert.AreEqual(
                MqttProtocolVersion.V311,
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535)), Serialize(packet, MqttProtocolVersion.V311)));

            Assert.AreEqual(
                MqttProtocolVersion.V500,
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535)), Serialize(packet, MqttProtocolVersion.V500)));

            var adapter = new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535));

            var ex = Assert.ThrowsException<MqttProtocolViolationException>(
                () => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[0])));
            Assert.AreEqual("CONNECT packet must have at least 7 bytes.", ex.Message);
            ex = Assert.ThrowsException<MqttProtocolViolationException>(
                () => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[7])));
            Assert.AreEqual("Protocol '' not supported.", ex.Message);
            ex = Assert.ThrowsException<MqttProtocolViolationException>(
                () => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[] { 255, 255, 0, 0, 0, 0, 0 })));
            Assert.AreEqual("Expected at least 65537 bytes but there are only 7 bytes", ex.Message);
        }

        [TestMethod]
        public void Serialize_LargePacket()
        {
            const int payloadLength = 80000;

            var payload = new byte[payloadLength];

            var value = 0;
            for (var i = 0; i < payloadLength; i++)
            {
                if (value > 255)
                {
                    value = 0;
                }

                payload[i] = (byte)value;
            }

            var publishPacket = new MqttPublishPacket
            {
                Topic = "abcdefghijklmnopqrstuvwxyz0123456789",
                PayloadSegment = payload
            };

            var serializationHelper = new MqttPacketSerializationHelper();

            var buffer = serializationHelper.Encode(publishPacket);
            var publishPacketCopy = serializationHelper.Decode(buffer) as MqttPublishPacket;

            Assert.IsNotNull(publishPacketCopy);
            Assert.AreEqual(publishPacket.Topic, publishPacketCopy.Topic);
            CollectionAssert.AreEqual(publishPacket.Payload.ToArray(), publishPacketCopy.Payload.ToArray());

            // Now modify the payload and test again.
            publishPacket.PayloadSegment = Encoding.UTF8.GetBytes("MQTT");

            buffer = serializationHelper.Encode(publishPacket);
            var publishPacketCopy2 = serializationHelper.Decode(buffer) as MqttPublishPacket;

            Assert.IsNotNull(publishPacketCopy2);
            Assert.AreEqual(publishPacket.Topic, publishPacketCopy2.Topic);
            CollectionAssert.AreEqual(publishPacket.Payload.ToArray(), publishPacketCopy2.Payload.ToArray());
        }

        [TestMethod]
        public void SerializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            SerializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
        }

        [TestMethod]
        public void SerializeV310_MqttConnectPacket()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true
            };

            SerializeAndCompare(p, "EB0ABk1RSXNkcAPCAHsAA1hZWgAEVVNFUgAEUEFTUw==", MqttProtocolVersion.V310);
        }

        [TestMethod]
        public void SerializeV311_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                IsSessionPresent = true,
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            SerializeAndCompare(p, "IAIBBQ==");
        }

        [TestMethod]
        public void SerializeV311_MqttConnectPacket()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true
            };

            SerializeAndCompare(p, "EBsABE1RVFQEwgB7AANYWVoABFVTRVIABFBBU1M=");
        }

        [TestMethod]
        public void SerializeV311_MqttConnectPacketWithWillMessage()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = Encoding.UTF8.GetBytes("PASS"),
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true,
                WillFlag = true,
                WillTopic = "My/last/will",
                WillMessage = Encoding.UTF8.GetBytes("Good byte."),
                WillQoS = MqttQualityOfServiceLevel.AtLeastOnce,
                WillRetain = true
            };

            SerializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
        }

        [TestMethod]
        public void SerializeV311_MqttDisconnectPacket()
        {
            SerializeAndCompare(new MqttDisconnectPacket(), "4AA=");
        }

        [TestMethod]
        public void SerializeV311_MqttPingReqPacket()
        {
            SerializeAndCompare(new MqttPingReqPacket(), "wAA=");
        }

        [TestMethod]
        public void SerializeV311_MqttPingRespPacket()
        {
            SerializeAndCompare(new MqttPingRespPacket(), "0AA=");
        }

        [TestMethod]
        public void SerializeV311_MqttPubAckPacket()
        {
            var p = new MqttPubAckPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "QAIAew==");
        }

        [TestMethod]
        public void SerializeV311_MqttPubCompPacket()
        {
            var p = new MqttPubCompPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "cAIAew==");
        }

        [TestMethod]
        public void SerializeV311_MqttPublishPacket()
        {
            var p = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                PayloadSegment = Encoding.ASCII.GetBytes("HELLO"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "A/B/C"
            };

            SerializeAndCompare(p, "Ow4ABUEvQi9DAHtIRUxMTw==");
        }

        [TestMethod]
        public void SerializeV311_MqttPubRecPacket()
        {
            var p = new MqttPubRecPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "UAIAew==");
        }

        [TestMethod]
        public void SerializeV311_MqttPubRelPacket()
        {
            var p = new MqttPubRelPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "YgIAew==");
        }

        [TestMethod]
        public void SerializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123,
                ReasonCodes = new List<MqttSubscribeReasonCode>
                {
                    MqttSubscribeReasonCode.GrantedQoS0,
                    MqttSubscribeReasonCode.GrantedQoS1,
                    MqttSubscribeReasonCode.GrantedQoS2,
                    MqttSubscribeReasonCode.UnspecifiedError
                }
            };

            SerializeAndCompare(p, "kAYAewABAoA=");
        }

        [TestMethod]
        public void SerializeV311_MqttSubscribePacket()
        {
            var p = new MqttSubscribePacket
            {
                PacketIdentifier = 123
            };

            p.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce });
            p.TopicFilters.Add(new MqttTopicFilter { Topic = "1/2/3", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
            p.TopicFilters.Add(new MqttTopicFilter { Topic = "x/y/z", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

            SerializeAndCompare(p, "ghoAewAFQS9CL0MCAAUxLzIvMwEABXgveS96AA==");
        }

        [TestMethod]
        public void SerializeV311_MqttUnsubAckPacket()
        {
            var p = new MqttUnsubAckPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "sAIAew==");
        }

        [TestMethod]
        public void SerializeV311_MqttUnsubscribePacket()
        {
            var p = new MqttUnsubscribePacket
            {
                PacketIdentifier = 123
            };

            p.TopicFilters.Add("A/B/C");
            p.TopicFilters.Add("1/2/3");
            p.TopicFilters.Add("x/y/z");

            SerializeAndCompare(p, "ohcAewAFQS9CL0MABTEvMi8zAAV4L3kveg==");
        }


        void DeserializeAndCompare(MqttPacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var writer = WriterFactory();

            var serializer = MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, writer);
            var buffer1 = serializer.Encode(packet);

            using (var headerStream = new MemoryStream(buffer1.Join().ToArray()))
            {
                using (var channel = new MemoryMqttChannel(headerStream))
                {
                    using (var adapter = new MqttChannelAdapter(
                               channel,
                               new MqttPacketFormatterAdapter(protocolVersion, new MqttBufferWriter(4096, 65535)),
                               new MqttNetEventLogger()))
                    {
                        var receivedPacket = adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();

                        var buffer2 = serializer.Encode(receivedPacket);

                        Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(buffer2.Join().ToArray()));
                    }
                }
            }
        }

        MqttProtocolVersion DeserializeAndDetectVersion(MqttPacketFormatterAdapter packetFormatterAdapter, byte[] buffer)
        {
            var channel = new MemoryMqttChannel(buffer);
            var adapter = new MqttChannelAdapter(channel, packetFormatterAdapter, new MqttNetEventLogger());

            adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            return packetFormatterAdapter.ProtocolVersion;
        }

        TPacket Roundtrip<TPacket>(TPacket packet, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311, MqttBufferReader bufferReader = null, MqttBufferWriter bufferWriter = null) where TPacket : MqttPacket
        {
            var writer = bufferWriter ?? WriterFactory();
            var serializer = MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, writer);
            var buffer = serializer.Encode(packet);

            using (var channel = new MemoryMqttChannel(buffer.Join().ToArray()))
            {
                var adapter = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(protocolVersion, new MqttBufferWriter(4096, 65535)), new MqttNetEventLogger());
                return (TPacket)adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        byte[] Serialize(MqttPacket packet, MqttProtocolVersion protocolVersion)
        {
            return MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, WriterFactory()).Encode(packet).Join().ToArray();
        }

        void SerializeAndCompare(MqttPacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(Serialize(packet, protocolVersion)));
        }

        MqttBufferWriter WriterFactory()
        {
            return new MqttBufferWriter(4096, 65535);
        }
    }
}