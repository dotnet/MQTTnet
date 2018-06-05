using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Adapter;
using MQTTnet.Core.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketSerializerTests
    {
        [TestMethod]
        public void SerializeV310_MqttConnectPacket()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = "PASS",
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
                Password = "PASS",
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
                Password = "PASS",
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true,
                WillMessage = new MqttApplicationMessage
                {
                    Topic = "My/last/will",
                    Payload = Encoding.UTF8.GetBytes("Good byte."),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = true
                }
            };

            SerializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
        }

        [TestMethod]
        public void DeserializeV311_MqttConnectPacket()
        {
            var p = new MqttConnectPacket
            {
                ClientId = "XYZ",
                Password = "PASS",
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
                Password = "PASS",
                Username = "USER",
                KeepAlivePeriod = 123,
                CleanSession = true,
                WillMessage = new MqttApplicationMessage
                {
                    Topic = "My/last/will",
                    Payload = Encoding.UTF8.GetBytes("Good byte."),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = true
                }
            };

            DeserializeAndCompare(p, "EDUABE1RVFQE7gB7AANYWVoADE15L2xhc3Qvd2lsbAAKR29vZCBieXRlLgAEVVNFUgAEUEFTUw==");
        }

        [TestMethod]
        public void SerializeV311_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                IsSessionPresent = true,
                ConnectReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            SerializeAndCompare(p, "IAIBBQ==");
        }

        [TestMethod]
        public void SerializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ConnectReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            SerializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
        }

        [TestMethod]
        public void DeserializeV311_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                IsSessionPresent = true,
                ConnectReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            DeserializeAndCompare(p, "IAIBBQ==");
        }

        [TestMethod]
        public void DeserializeV310_MqttConnAckPacket()
        {
            var p = new MqttConnAckPacket
            {
                ConnectReturnCode = MqttConnectReturnCode.ConnectionRefusedNotAuthorized
            };

            DeserializeAndCompare(p, "IAIABQ==", MqttProtocolVersion.V310);
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

            p.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce));
            p.TopicFilters.Add(new TopicFilter("1/2/3", MqttQualityOfServiceLevel.AtLeastOnce));
            p.TopicFilters.Add(new TopicFilter("x/y/z", MqttQualityOfServiceLevel.AtMostOnce));

            SerializeAndCompare(p, "ghoAewAFQS9CL0MCAAUxLzIvMwEABXgveS96AA==");
        }

        [TestMethod]
        public void DeserializeV311_MqttSubscribePacket()
        {
            var p = new MqttSubscribePacket
            {
                PacketIdentifier = 123
            };

            p.TopicFilters.Add(new TopicFilter("A/B/C", MqttQualityOfServiceLevel.ExactlyOnce));
            p.TopicFilters.Add(new TopicFilter("1/2/3", MqttQualityOfServiceLevel.AtLeastOnce));
            p.TopicFilters.Add(new TopicFilter("x/y/z", MqttQualityOfServiceLevel.AtMostOnce));

            DeserializeAndCompare(p, "ghoAewAFQS9CL0MCAAUxLzIvMwEABXgveS96AA==");
        }

        [TestMethod]
        public void SerializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123
            };

            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS0);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS1);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS2);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);

            SerializeAndCompare(p, "kAYAewABAoA=");
        }

        [TestMethod]
        public void DeserializeV311_MqttSubAckPacket()
        {
            var p = new MqttSubAckPacket
            {
                PacketIdentifier = 123
            };

            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS0);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS1);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.SuccessMaximumQoS2);
            p.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);

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

        private static void SerializeAndCompare(MqttBasePacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var serializer = new MqttPacketSerializer { ProtocolVersion = protocolVersion };
            var data = serializer.Serialize(packet);
            
            Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(Join(data)));
        }

        private static void DeserializeAndCompare(MqttBasePacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var serializer = new MqttPacketSerializer { ProtocolVersion = protocolVersion };

            var buffer1 = serializer.Serialize(packet);

            using (var headerStream = new MemoryStream(Join(buffer1)))
            {
                var channel = new TestMqttChannel(headerStream);
                var header = MqttPacketReader.ReadFixedHeaderAsync(channel, CancellationToken.None).GetAwaiter().GetResult();
                
                using (var bodyStream = new MemoryStream(Join(buffer1), (int)headerStream.Position, header.RemainingLength))
                {
                    var deserializedPacket = serializer.Deserialize(new ReceivedMqttPacket(header.Flags, bodyStream));
                    var buffer2 = serializer.Serialize(deserializedPacket);

                    Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(Join(buffer2)));
                }
            }
        }

        private static byte[] Join(params ArraySegment<byte>[] chunks)
        {
            var buffer = new MemoryStream();
            foreach (var chunk in chunks)
            {
                buffer.Write(chunk.Array, chunk.Offset, chunk.Count);
            }

            return buffer.ToArray();
        }
    }
}
