using System;
using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Serializer
{
    public class MqttV500PacketSerializer : MqttV311PacketSerializer
    {
        //public ArraySegment<byte> Serialize(MqttBasePacket mqttPacket)
        //{
        //    throw new NotImplementedException();
        //}

        //public MqttBasePacket Deserialize(ReceivedMqttPacket receivedMqttPacket)
        //{
        //    throw new NotImplementedException();
        //}

        protected override byte SerializeConnectPacket(MqttConnectPacket packet, MqttPacketWriter packetWriter)
        {
            ValidateConnectPacket(packet);

            packetWriter.WriteWithLengthPrefix("MQTT");
            packetWriter.Write(5); // [3.1.2.2 Protocol Version]

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

            if (packet.Properties != null)
            {
                var propertyWriter = new MqttPacketWriter();
                foreach (var property in packet.Properties)
                {
                    propertyWriter.Write(property.Id);
                    property.WriteTo(propertyWriter);
                }

                packetWriter.WriteVariableLengthInteger((uint)propertyWriter.Length);
                packetWriter.Write(propertyWriter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }
    }
}
