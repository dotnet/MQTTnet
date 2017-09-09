using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Serializer;

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

            SerializeAndCompare(p, "EB0ABE1RSXNkcAPCAHsAA1hZWgAEVVNFUgAEUEFTUw==", MqttProtocolVersion.V310);
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
                WillMessage = new MqttApplicationMessage(
                    "My/last/will",
                    Encoding.UTF8.GetBytes("Good byte."),
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    true)
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
                WillMessage = new MqttApplicationMessage(
                    "My/last/will",
                    Encoding.UTF8.GetBytes("Good byte."),
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    true)
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
                ConnectReturnCode = MqttConnectReturnCode.ConnectionAccepted
            };

            SerializeAndCompare(p, "IAIAAA==", MqttProtocolVersion.V310);
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


        public class TestChannel : IMqttCommunicationChannel
        {
            private readonly MemoryStream _stream = new MemoryStream();

            public bool IsConnected { get; } = true;

            public TestChannel()
            {
            }

            public TestChannel(byte[] initialData)
            {
                _stream.Write(initialData, 0, initialData.Length);
                _stream.Position = 0;
            }

            public Task ConnectAsync(MqttClientOptions options)
            {
                return Task.FromResult(0);
            }

            public Task DisconnectAsync()
            {
                return Task.FromResult(0);
            }

            public Task WriteAsync(byte[] buffer)
            {
                return _stream.WriteAsync(buffer, 0, buffer.Length);
            }

            public Task ReadAsync(byte[] buffer)
            {
                return _stream.ReadAsync(buffer, 0, buffer.Length);
            }

            public byte[] ToArray()
            {
                return _stream.ToArray();
            }
        }

        private static void SerializeAndCompare(MqttBasePacket packet, string expectedBase64Value, MqttProtocolVersion protocolVersion = MqttProtocolVersion.V311)
        {
            var serializer = new MqttPacketSerializer { ProtocolVersion = protocolVersion };
            var channel = new TestChannel();
            serializer.SerializeAsync(packet, channel).Wait();
            var buffer = channel.ToArray();

            Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(buffer));
        }

        private static void DeserializeAndCompare(MqttBasePacket packet, string expectedBase64Value)
        {
            var serializer = new MqttPacketSerializer();

            var channel1 = new TestChannel();
            serializer.SerializeAsync(packet, channel1).Wait();
            var buffer1 = channel1.ToArray();

            var channel2 = new TestChannel(buffer1);
            var deserializedPacket = serializer.DeserializeAsync(channel2).Result;
            var buffer2 = channel2.ToArray();

            var channel3 = new TestChannel(buffer2);
            serializer.SerializeAsync(deserializedPacket, channel3).Wait();

            Assert.AreEqual(expectedBase64Value, Convert.ToBase64String(channel3.ToArray()));
        }
    }
}
