using System;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V500
{
    public class MqttV500PacketEncoder
    {
        public byte EncodeConnectPacket(MqttConnectPacket packet, MqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (string.IsNullOrEmpty(packet.ClientId) && !packet.CleanSession)
            {
                throw new MqttProtocolViolationException("CleanSession must be set if ClientId is empty [MQTT-3.1.3-7].");
            }

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

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.WillDelayInterval, packet.Properties.WillDelayInterval);
                propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.SessionExpiryInterval, packet.Properties.SessionExpiryInterval);
                propertiesWriter.Write(MqttPropertyID.AuthenticationMethod, packet.Properties.AuthenticationMethod);
                propertiesWriter.Write(MqttPropertyID.AuthenticationData, packet.Properties.AuthenticationData);
                propertiesWriter.Write(MqttPropertyID.RequestProblemInformation, packet.Properties.RequestProblemInformation);
                propertiesWriter.Write(MqttPropertyID.RequestResponseInformation, packet.Properties.RequestResponseInformation);
                propertiesWriter.Write(MqttPropertyID.ReceiveMaximum, packet.Properties.ReceiveMaximum);
                propertiesWriter.Write(MqttPropertyID.TopicAlias, packet.Properties.TopicAliasMaximum);
                propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.MaximumPacketSize, packet.Properties.MaximumPacketSize);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
                propertiesWriter.WriteToPacket(packetWriter);
            }

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

        public byte EncodeConnAckPacket(MqttConnAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (!packet.ReasonCode.HasValue)
            {
                throw new MqttProtocolViolationException("The ReasonCode must be set for MQTT version 5.");
            }

            byte connectAcknowledgeFlags = 0x0;
            if (packet.IsSessionPresent)
            {
                connectAcknowledgeFlags |= 0x1;
            }

            packetWriter.Write(connectAcknowledgeFlags);
            packetWriter.Write((byte)packet.ConnectReturnCode);
            packetWriter.Write((byte)packet.ReasonCode.Value);

            var propertiesWriter = new MqttV500PropertiesWriter();
            if (packet.Properties != null)
            {
                propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.SessionExpiryInterval, packet.Properties.SessionExpiryInterval);
                propertiesWriter.Write(MqttPropertyID.AuthenticationMethod, packet.Properties.AuthenticationMethod);
                propertiesWriter.Write(MqttPropertyID.AuthenticationData, packet.Properties.AuthenticationData);
                propertiesWriter.Write(MqttPropertyID.RetainAvailable, packet.Properties.RetainAvailable);
                propertiesWriter.Write(MqttPropertyID.ReceiveMaximum, packet.Properties.ReceiveMaximum);
                propertiesWriter.Write(MqttPropertyID.AssignedClientIdentifer, packet.Properties.AssignedClientIdentifier);
                propertiesWriter.Write(MqttPropertyID.TopicAliasMaximum, packet.Properties.TopicAliasMaximum);
                propertiesWriter.Write(MqttPropertyID.ReasonString, packet.Properties.ReasonString);
                propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.MaximumPacketSize, packet.Properties.MaximumPacketSize);
                propertiesWriter.Write(MqttPropertyID.WildcardSubscriptionAvailable, packet.Properties.WildcardSubscriptionAvailable);
                propertiesWriter.Write(MqttPropertyID.SubscriptionIdentifierAvailable, packet.Properties.SubscriptionIdentifiersAvailable);
                propertiesWriter.Write(MqttPropertyID.SharedSubscriptionAvailable, packet.Properties.SharedSubscriptionAvailable);
                propertiesWriter.Write(MqttPropertyID.ServerKeepAlive, packet.Properties.ServerKeepAlive);
                propertiesWriter.Write(MqttPropertyID.ResponseInformation, packet.Properties.ResponseInformation);
                propertiesWriter.Write(MqttPropertyID.ServerReference, packet.Properties.ServerReference);
                propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
                propertiesWriter.WriteToPacket(packetWriter);
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.ConnAck);
        }

        public byte EncodePublishPacket(MqttPublishPacket packet, MqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (packet.QualityOfServiceLevel == 0 && packet.Dup)
            {
                throw new MqttProtocolViolationException("Dup flag must be false for QoS 0 packets [MQTT-3.3.1-2].");
            }

            packetWriter.WriteWithLengthPrefix(packet.Topic);

            if (packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (!packet.PacketIdentifier.HasValue)
                {
                    throw new MqttProtocolViolationException("Publish packet has no packet identifier.");
                }

                packetWriter.Write(packet.PacketIdentifier.Value);
            }
            else
            {
                if (packet.PacketIdentifier > 0)
                {
                    throw new MqttProtocolViolationException("Packet identifier must be empty if QoS == 0 [MQTT-2.3.1-5].");
                }
            }

            var propertiesWriter = new MqttV500PropertiesWriter();
            propertiesWriter.Write(MqttPropertyID.PayloadFormatIndicator, packet.Properties.PayloadFormatIndicator);
            propertiesWriter.WriteAsFourByteInteger(MqttPropertyID.MessageExpiryInterval, packet.Properties.MessageExpiryInterval);
            propertiesWriter.Write(MqttPropertyID.TopicAlias, packet.Properties.TopicAlias);
            propertiesWriter.Write(MqttPropertyID.ResponseTopic, packet.Properties.ResponseTopic);
            propertiesWriter.Write(MqttPropertyID.CorrelationData, packet.Properties.CorrelationData);
            propertiesWriter.WriteAsVariableLengthInteger(MqttPropertyID.SubscriptionIdentifier, packet.Properties.SubscriptionIdentifier);
            propertiesWriter.Write(MqttPropertyID.ContentType, packet.Properties.ContentType);
            propertiesWriter.WriteUserProperties(packet.Properties.UserProperties);
            propertiesWriter.WriteToPacket(packetWriter);

            if (packet.Payload?.Length > 0)
            {
                packetWriter.Write(packet.Payload, 0, packet.Payload.Length);
            }

            byte fixedHeader = 0;

            if (packet.Retain)
            {
                fixedHeader |= 0x01;
            }

            fixedHeader |= (byte)((byte)packet.QualityOfServiceLevel << 1);

            if (packet.Dup)
            {
                fixedHeader |= 0x08;
            }

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.Publish, fixedHeader);
        }

        public byte EncodePubAckPacket(MqttPubAckPacket packet, MqttPacketWriter packetWriter)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (packetWriter == null) throw new ArgumentNullException(nameof(packetWriter));

            if (!packet.PacketIdentifier.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet has no packet identifier.");
            }

            if (!packet.ReasonCode.HasValue)
            {
                throw new MqttProtocolViolationException("PubAck packet must contain a connect reason.");
            }

            packetWriter.Write(packet.PacketIdentifier.Value);
            packetWriter.Write((byte)packet.ReasonCode.Value);

            return MqttPacketWriter.BuildFixedHeader(MqttControlPacketType.PubAck);
        }
    }
}
