using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V3
{
    public sealed class MqttV311PacketFormatter : MqttV310PacketFormatter
    {
        public MqttV311PacketFormatter(IMqttPacketWriter packetWriter)
            : base(packetWriter)
        {
        }

        protected override byte EncodeConnectPacket(MqttConnectPacket packet, IMqttPacketWriter packetWriter)
        {
            ValidateConnectPacket(packet);

            packetWriter.WriteWithLengthPrefix("MQTT");
            packetWriter.Write(4); // 3.1.2.2 Protocol Level 4

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

        protected override byte EncodeConnAckPacket(MqttConnAckPacket packet, IMqttPacketWriter packetWriter)
        {
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            packetWriter.Write(connectAcknowledgeFlags);
            packetWriter.Write((byte)packet.ReturnCode);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        protected override MqttBasePacket DecodeConnAckPacket(IMqttPacketBodyReader body)
        {
            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = body.ReadByte();

            packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            packet.ReturnCode = (MqttConnectReturnCode)body.ReadByte();

            return packet;
        }
    }
}
