using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Formatter.V3;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Tests.Extensions;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttPacketSerializer_Tests
    {
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
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttPacketWriter()), Serialize(packet, MqttProtocolVersion.V310)));

            Assert.AreEqual(
                MqttProtocolVersion.V311,
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttPacketWriter()), Serialize(packet, MqttProtocolVersion.V311)));

            Assert.AreEqual(
                MqttProtocolVersion.V500,
                DeserializeAndDetectVersion(new MqttPacketFormatterAdapter(new MqttPacketWriter()), Serialize(packet, MqttProtocolVersion.V500)));

            var adapter = new MqttPacketFormatterAdapter(new MqttPacketWriter());

            var ex = Assert.ThrowsException<MqttProtocolViolationException>(() => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[0])));
            Assert.AreEqual("CONNECT packet must have at least 7 bytes.", ex.Message);
            ex = Assert.ThrowsException<MqttProtocolViolationException>(() => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[7])));
            Assert.AreEqual("Protocol '' not supported.", ex.Message);
            ex = Assert.ThrowsException<MqttProtocolViolationException>(() => DeserializeAndDetectVersion(adapter, WriterFactory().AddMqttHeader(MqttControlPacketType.Connect, new byte[] { 255, 255, 0, 0, 0, 0, 0 })));
            Assert.AreEqual("Expected at least 65537 bytes but there are only 7 bytes", ex.Message);
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
                WillQoS =  MqttQualityOfServiceLevel.AtLeastOnce,
                WillRetain = true
            };

            SerializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
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
                WillQoS =  MqttQualityOfServiceLevel.AtLeastOnce,
                WillRetain = true
            };

            DeserializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
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
        public void SerializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            SerializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
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
        public void DeserializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            DeserializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
        }

        [TestMethod]
        public void Serialize_LargePacket()
        {
            var serializer = new MqttV311PacketFormatter(WriterFactory());

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
                Payload = payload
            };


            var publishPacketCopy = Roundtrip(publishPacket);

            //var buffer = serializer.Encode(publishPacket);
            //var testChannel = new TestMqttChannel(new MemoryStream(buffer.Array, buffer.Offset, buffer.Count));


            //var header = new MqttPacketReader(testChannel).ReadFixedHeaderAsync(
            //    new byte[2],
            //    CancellationToken.None).GetAwaiter().GetResult().FixedHeader;

            //var eof = buffer.Offset + buffer.Count;

            //var receivedPacket = new ReceivedMqttPacket(
            //    header.Flags,
            //    new MqttPacketBodyReader(buffer.Array, eof - header.RemainingLength, buffer.Count + buffer.Offset),
            //    0);

            //var packet = (MqttPublishPacket)serializer.Decode(receivedPacket);

            Assert.AreEqual(publishPacket.Topic, publishPacketCopy.Topic);
            Assert.IsTrue(publishPacket.Payload.SequenceEqual(publishPacketCopy.Payload));
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
        public void SerializeV311_MqttPublishPacket()
        {
            var p = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                Payload = Encoding.ASCII.GetBytes("HELLO"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "A/B/C"
            };

            SerializeAndCompare(p, "Ow4ABUEvQi9DAHtIRUxMTw==");
        }

        [TestMethod]
        public void SerializeV500_MqttPublishPacket()
        {
            var p = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                Payload = Encoding.ASCII.GetBytes("HELLO"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "A/B/C",
                Properties =
                {
                    ResponseTopic = "/Response"
                }
            };

            p.Properties.UserProperties.Add(new MqttUserProperty("Foo", "Bar"));

            var deserialized = Roundtrip(p, MqttProtocolVersion.V500);

            Assert.AreEqual(p.Properties.ResponseTopic, deserialized.Properties.ResponseTopic);
            Assert.IsTrue(deserialized.Properties.UserProperties.Any(x => x.Name == "Foo"));
        }


        [TestMethod]
        public void SerializeV500_MqttPublishPacket_CorrelationData()
        {
            var data = "123456789";
            var req = new MqttApplicationMessageBuilder()
                      .WithTopic("Foo")
                      .WithResponseTopic($"_")
                      .WithCorrelationData(Guid.NewGuid().ToByteArray())
                      .WithPayload(data)
                      .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                      .Build();

            var p = new MqttPublishPacketFactory().Create(req);

            var deserialized = Roundtrip(p, MqttProtocolVersion.V500);

            Assert.IsTrue(p.Payload.SequenceEqual(deserialized.Payload));
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket()
        {
            var p = new MqttPublishPacket
            {
                PacketIdentifier = 123,
                Dup = true,
                Retain = true,
                Payload = Encoding.ASCII.GetBytes("HELLO"),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Topic = "A/B/C"
            };

            DeserializeAndCompare(p, "Ow4ABUEvQi9DAHtIRUxMTw==");
        }

        [TestMethod]
        public void DeserializeV311_MqttPublishPacket_Qos1()
        {
            var p = new MqttPublishPacket
            {
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
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
        public void DeserializeV311_MqttPublishPacket_DupFalse()
        {
            var p = new MqttPublishPacket
            {
                Dup = false,
            };

            var p2 = Roundtrip(p);

            Assert.AreEqual(p.Dup, p2.Dup);
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
        public void DeserializeV311_MqttPubAckPacket()
        {
            var p = new MqttPubAckPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "QAIAew==");
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
        public void DeserializeV311_MqttPubRecPacket()
        {
            var p = new MqttPubRecPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "UAIAew==");
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
        public void DeserializeV311_MqttPubRelPacket()
        {
            var p = new MqttPubRelPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "YgIAew==");
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
        public void DeserializeV311_MqttPubCompPacket()
        {
            var p = new MqttPubCompPacket
            {
                PacketIdentifier = 123
            };

            DeserializeAndCompare(p, "cAIAew==");
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
        public void DeserializeV311_MqttSubscribePacket()
        {
            var p = new MqttSubscribePacket
            {
                PacketIdentifier = 123
            };

            p.TopicFilters.Add(new MqttTopicFilter { Topic = "A/B/C", QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce });
            p.TopicFilters.Add(new MqttTopicFilter { Topic = "1/2/3", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce });
            p.TopicFilters.Add(new MqttTopicFilter { Topic = "x/y/z", QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce });

            DeserializeAndCompare(p, "ghoAewAFQS9CL0MCAAUxLzIvMwEABXgveS96AA==");
        }

        [TestMethod]
        public void SerializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123
            };

            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS0);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS1);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS2);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.UnspecifiedError);

            SerializeAndCompare(p, "kAYAewABAoA=");
        }

        [TestMethod]
        public void DeserializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123
            };

            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS0);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS1);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.GrantedQoS2);
            p.ReasonCodes.Add(MqttSubscribeReasonCode.UnspecifiedError);

            DeserializeAndCompare(p, "kAYAewABAoA=");
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
        public void SerializeV311_MqttUnsubAckPacket()
        {
            var p = new MqttUnsubAckPacket
            {
                PacketIdentifier = 123
            };

            SerializeAndCompare(p, "sAIAew==");
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

        void SerializeAndCompare(MqttBasePacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(Serialize(packet, protocolVersion)));
        }

        byte[] Serialize(MqttBasePacket packet, MqttProtocolVersion protocolVersion)
        {
            return MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, WriterFactory()).Encode(packet).ToArray().ToArray();
        }

        protected virtual IMqttPacketWriter WriterFactory()
        {
            return new MqttPacketWriter();
        }

        protected virtual IMqttPacketBodyReader ReaderFactory(byte[] data)
        {
            return new MqttPacketBodyReader(data, 0, data.Length);
        }

        void DeserializeAndCompare(MqttBasePacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var writer = WriterFactory();

            var serializer = MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, writer);
            var buffer1 = serializer.Encode(packet);

            using (var headerStream = new MemoryStream(buffer1.ToArray().ToArray()))
            {
                var channel = new TestMqttChannel(headerStream);
                var adapter = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(protocolVersion, writer), null, new MqttNetEventLogger());
                var receivedPacket = adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();

                var buffer2 = serializer.Encode(receivedPacket);

                Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(buffer2.ToArray().ToArray()));
            }
        }

        TPacket Roundtrip<TPacket>(TPacket packet, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
            where TPacket : MqttBasePacket
        {
            var writer = WriterFactory();
            var serializer = MqttPacketFormatterAdapter.GetMqttPacketFormatter(protocolVersion, writer);
            var buffer = serializer.Encode(packet);
            
            var channel = new TestMqttChannel(buffer.ToArray().ToArray());
            var adapter = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(protocolVersion, writer), null, new MqttNetEventLogger());
            return (TPacket)adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        MqttProtocolVersion DeserializeAndDetectVersion(MqttPacketFormatterAdapter packetFormatterAdapter, byte[] buffer)
        {
            var channel = new TestMqttChannel(buffer);
            var adapter = new MqttChannelAdapter(channel, packetFormatterAdapter, null, new MqttNetEventLogger());

            adapter.ReceivePacketAsync(CancellationToken.None).GetAwaiter().GetResult();
            return packetFormatterAdapter.ProtocolVersion;
        }
    }
}
