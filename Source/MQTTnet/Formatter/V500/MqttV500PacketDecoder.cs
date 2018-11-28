using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V500
{
    public class MqttV500PacketDecoder
    {
        public MqttBasePacket DecodeConnAckPacket(MqttPacketBodyReader body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            ThrowIfBodyIsEmpty(body);

            var packet = new MqttConnAckPacket();

            var acknowledgeFlags = body.ReadByte();

            packet.IsSessionPresent = (acknowledgeFlags & 0x1) > 0;
            packet.ConnectReturnCode = (MqttConnectReturnCode)body.ReadByte();
            packet.ReasonCode = (MqttConnectReasonCode)body.ReadByte();

            var propertiesLength = body.ReadVariableLengthInteger();
            if (propertiesLength > 0)
            {
                packet.Properties = new MqttConnAckPacketProperties();

                while (!body.EndOfStream)
                {
                    var propertyID = (MqttPropertyID)body.ReadByte();

                    if (propertyID == MqttPropertyID.SessionExpiryInterval)
                    {
                        packet.Properties.SessionExpiryInterval = body.ReadFourByteInteger();
                    }
                    else if (propertyID == MqttPropertyID.AuthenticationMethod)
                    {
                        packet.Properties.AuthenticationMethod = body.ReadStringWithLengthPrefix();
                    }
                    else if (propertyID == MqttPropertyID.AuthenticationData)
                    {
                        packet.Properties.AuthenticationData = body.ReadWithLengthPrefix().ToArray();
                    }
                    else if (propertyID == MqttPropertyID.RetainAvailable)
                    {
                        packet.Properties.RetainAvailable = body.ReadBoolean();
                    }
                    else if (propertyID == MqttPropertyID.ReceiveMaximum)
                    {
                        packet.Properties.ReceiveMaximum = body.ReadTwoByteInteger();
                    }
                    else if (propertyID == MqttPropertyID.AssignedClientIdentifer)
                    {
                        packet.Properties.AssignedClientIdentifier = body.ReadStringWithLengthPrefix();
                    }
                    else if (propertyID == MqttPropertyID.TopicAliasMaximum)
                    {
                        packet.Properties.TopicAliasMaximum = body.ReadTwoByteInteger();
                    }
                    else if (propertyID == MqttPropertyID.ReasonString)
                    {
                        packet.Properties.ReasonString = body.ReadStringWithLengthPrefix();
                    }
                    else if (propertyID == MqttPropertyID.MaximumPacketSize)
                    {
                        packet.Properties.MaximumPacketSize = body.ReadFourByteInteger();
                    }
                    else if (propertyID == MqttPropertyID.WildcardSubscriptionAvailable)
                    {
                        packet.Properties.WildcardSubscriptionAvailable = body.ReadBoolean();
                    }
                    else if (propertyID == MqttPropertyID.SubscriptionIdentifierAvailable)
                    {
                        packet.Properties.SubscriptionIdentifiersAvailable = body.ReadBoolean();
                    }
                    else if (propertyID == MqttPropertyID.SharedSubscriptionAvailable)
                    {
                        packet.Properties.SharedSubscriptionAvailable = body.ReadBoolean();
                    }
                    else if (propertyID == MqttPropertyID.ServerKeepAlive)
                    {
                        packet.Properties.ServerKeepAlive = body.ReadTwoByteInteger();
                    }
                    else if (propertyID == MqttPropertyID.ResponseInformation)
                    {
                        packet.Properties.ResponseInformation = body.ReadStringWithLengthPrefix();
                    }
                    else if (propertyID == MqttPropertyID.ServerReference)
                    {
                        packet.Properties.ServerReference = body.ReadStringWithLengthPrefix();
                    }
                    else if (propertyID == MqttPropertyID.UserProperty)
                    {
                        packet.Properties.UserProperties = ReadUserProperties(body);
                    }
                }
            }

            return packet;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowIfBodyIsEmpty(MqttPacketBodyReader body)
        {
            if (body == null || body.Length == 0)
            {
                throw new MqttProtocolViolationException("Data from the body is required but not present.");
            }
        }

        private static List<MqttUserProperty> ReadUserProperties(MqttPacketBodyReader reader)
        {
            var userPropertiesLength = reader.ReadVariableLengthInteger();
            if (userPropertiesLength == 0)
            {
                return new List<MqttUserProperty>();
            }

            var userProperties = new List<MqttUserProperty>();
            var targetPosition = reader.Offset + userPropertiesLength;
            while (reader.Offset < targetPosition)
            {
                var name = reader.ReadStringWithLengthPrefix();
                var value = reader.ReadStringWithLengthPrefix();

                userProperties.Add(new MqttUserProperty(name, value));
            }

            return userProperties;
        }
    }
}
