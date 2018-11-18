using System;
using System.Collections.Generic;
using MQTTnet.Exceptions;
using MQTTnet.Formatter.V311;
using MQTTnet.Packets;
using MQTTnet.Packets.Properties;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V500
{
    public class MqttV500PacketFormatter : MqttV311PacketFormatter
    {
        private readonly MqttV500PacketEncoder _encoder = new MqttV500PacketEncoder();
        private readonly MqttV500PacketDecoder _decoder = new MqttV500PacketDecoder();

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

            WriteProperty(MqttMessagePropertyID.WillDelayInterval, packet.WillDelayIntervalProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.SessionExpiryInterval, packet.SessionExpiryIntervalProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.AuthenticationMethod, packet.AuthenticationMethodProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.AuthenticationData, packet.AuthenticationDataProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.RequestProblemInformation, packet.RequestProblemInformationProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.RequestResponseInformation, packet.RequestResponseInformationProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.ReceiveMaximum, packet.ReceiveMaximumProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.TopicAlias, packet.TopicAliasMaximumProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.MaximumPacketSize, packet.MaximumPacketSizeProperty, packetWriter);
            WriteUserProperties(packet.UserPropertiesProperty, packetWriter);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Connect);
        }

        protected override byte SerializeConnAckPacket(MqttConnAckPacket packet, MqttPacketWriter packetWriter)
        {
            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            packetWriter.Write(connectAcknowledgeFlags);
            packetWriter.Write((byte)packet.ConnectReturnCode);

            WriteProperty(MqttMessagePropertyID.SessionExpiryInterval, packet.SessionExpiryIntervalProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.AuthenticationMethod, packet.AuthenticationMethodProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.AuthenticationData, packet.AuthenticationDataProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.ReceiveMaximum, packet.ReceiveMaximumProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.TopicAlias, packet.TopicAliasMaximumProperty, packetWriter);
            WriteProperty(MqttMessagePropertyID.MaximumPacketSize, packet.MaximumPacketSizeProperty, packetWriter);
            WriteUserProperties(packet.UserPropertiesProperty, packetWriter);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        private static void WriteProperty(MqttMessagePropertyID id, bool? value, MqttPacketWriter target)
        {
            if (!value.HasValue)
            {
                return;
            }

            target.Write((byte)id);
            target.Write(value.Value ? (byte)0x1 : (byte)0x0);
        }

        private static void WriteProperty(MqttMessagePropertyID id, ushort? value, MqttPacketWriter target)
        {
            if (!value.HasValue)
            {
                return;
            }

            target.Write((byte)id);
            target.Write(value.Value);
        }

        private static void WriteProperty(MqttMessagePropertyID id, uint? value, MqttPacketWriter target)
        {
            if (!value.HasValue)
            {
                return;
            }

            // TODO: Check if order must be reversed like for ushort.
            target.Write((byte)id);
            var bytes = BitConverter.GetBytes(value.Value);
            target.Write(bytes, 0, bytes.Length);
        }

        private static void WriteProperty(MqttMessagePropertyID id, string value, MqttPacketWriter target)
        {
            if (value == null)
            {
                return;
            }
            
            target.Write((byte)id);
            target.WriteWithLengthPrefix(value);
        }

        private static void WriteProperty(MqttMessagePropertyID id, byte[] value, MqttPacketWriter target)
        {
            if (value == null)
            {
                return;
            }

            target.Write((byte)id);
            target.WriteWithLengthPrefix(value);
        }

        private static void WriteUserProperties(List<MqttUserProperty> userProperties, MqttPacketWriter target)
        {
            if (userProperties == null || userProperties.Count == 0)
            {
                return;
            }

            var propertyWriter = new MqttPacketWriter();
            foreach (var property in userProperties)
            {
                propertyWriter.WriteWithLengthPrefix(property.Name);
                propertyWriter.WriteWithLengthPrefix(property.Value);
            }

            target.Write((byte)MqttMessagePropertyID.UserProperty);
            target.WriteVariableLengthInteger((uint)propertyWriter.Length);
            target.Write(propertyWriter);
        }
    }
}
