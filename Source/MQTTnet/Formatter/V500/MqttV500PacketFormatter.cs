using System;
using System.Collections.Generic;
using MQTTnet.Formatter.V311;
using MQTTnet.Packets;

namespace MQTTnet.Formatter.V500
{
    public class MqttV500PacketFormatter : MqttV311PacketFormatter
    {
        private readonly MqttV500PacketEncoder _encoder = new MqttV500PacketEncoder();
        private readonly MqttV500PacketDecoder _decoder = new MqttV500PacketDecoder();

        public override MqttPublishPacket ConvertApplicationMessageToPublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false,
                Properties = new MqttPublishPacketProperties
                {
                    UserProperties = new List<MqttUserProperty>(applicationMessage.UserProperties)
                }
            };
        }

        protected override byte SerializeConnectPacket(MqttConnectPacket packet, MqttPacketWriter packetWriter)
        {
            return _encoder.EncodeConnectPacket(packet, packetWriter);
        }

        protected override byte SerializeConnAckPacket(MqttConnAckPacket packet, MqttPacketWriter packetWriter)
        {
            return _encoder.EncodeConnAckPacket(packet, packetWriter);
        }

        protected override MqttBasePacket DeserializeConnAckPacket(MqttPacketBodyReader body)
        {
            return _decoder.DecodeConnAckPacket(body);
        }

        protected override byte SerializePublishPacket(MqttPublishPacket packet, MqttPacketWriter packetWriter)
        {
            return _encoder.EncodePublishPacket(packet, packetWriter);
        }

        protected override byte SerializePubAckPacket(MqttPubAckPacket packet, MqttPacketWriter packetWriter)
        {
            return _encoder.EncodePubAckPacket(packet, packetWriter);
        }
    }
}
