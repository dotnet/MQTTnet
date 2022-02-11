// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V5
{
    public sealed class MqttV500PropertiesWriter
    {
        readonly MqttPacketWriter _packetWriter = new MqttPacketWriter();

        public int Length => _packetWriter.Length;

        public void WriteAssignedClientIdentifier(string value)
        {
            Write(MqttPropertyId.AssignedClientIdentifier, value);
        }

        public void WriteAuthenticationData(byte[] value)
        {
            Write(MqttPropertyId.AuthenticationData, value);
        }

        public void WriteAuthenticationMethod(string value)
        {
            Write(MqttPropertyId.AuthenticationMethod, value);
        }

        public void WriteContentType(string value)
        {
            Write(MqttPropertyId.ContentType, value);
        }

        public void WriteCorrelationData(byte[] value)
        {
            Write(MqttPropertyId.CorrelationData, value);
        }

        public void WriteMaximumPacketSize(uint value)
        {
            // It is a Protocol Error to include the Maximum Packet Size more than once, or for the value to be set to zero.
            if (value == 0)
            {
                return;
            }

            WriteAsFourByteInteger(MqttPropertyId.MaximumPacketSize, value);
        }

        public void WriteMaximumQoS(MqttQualityOfServiceLevel value)
        {
            // It is a Protocol Error to include Maximum QoS more than once, or to have a value other than 0 or 1. If the Maximum QoS is absent, the Client uses a Maximum QoS of 2.
            if (value == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                return;
            }
            
            if (value == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                Write(MqttPropertyId.MaximumQoS, true);
            }
            else
            {
                Write(MqttPropertyId.MaximumQoS, false);
            }
        }

        public void WriteMessageExpiryInterval(uint value)
        {
            // If absent, the Application Message does not expire.
            // This library uses 0 to indicate no expiration.
            WriteAsFourByteInteger(MqttPropertyId.MessageExpiryInterval, value);
        }

        public void WritePayloadFormatIndicator(MqttPayloadFormatIndicator value)
        {
            // 0 (0x00) Byte Indicates that the Payload is unspecified bytes, which is equivalent to not sending a Payload Format Indicator.
            if (value == MqttPayloadFormatIndicator.Unspecified)
            {
                return;
            }

            Write(MqttPropertyId.PayloadFormatIndicator, (byte)value);
        }

        public void WriteReasonString(string value)
        {
            Write(MqttPropertyId.ReasonString, value);
        }

        public void WriteReceiveMaximum(ushort value)
        {
            // It is a Protocol Error to include the Receive Maximum value more than once or for it to have the value 0.
            if (value == 0)
            {
                return;
            }

            Write(MqttPropertyId.ReceiveMaximum, value);
        }

        public void WriteRequestProblemInformation(bool value)
        {
            // If the Request Problem Information is absent, the value of 1 is used.
            if (value)
            {
                return;
            }

            Write(MqttPropertyId.RequestProblemInformation, false);
        }

        public void WriteRequestResponseInformation(bool value)
        {
            // If the Request Response Information is absent, the value of 0 is used.
            if (!value)
            {
                return;
            }

            Write(MqttPropertyId.RequestResponseInformation, true);
        }

        public void WriteResponseInformation(string value)
        {
            Write(MqttPropertyId.ResponseInformation, value);
        }

        public void WriteResponseTopic(string value)
        {
            Write(MqttPropertyId.ResponseTopic, value);
        }

        public void WriteRetainAvailable(bool value)
        {
            if (value)
            {
                // Absence of the flag means it is supported! 
                return;
            }

            Write(MqttPropertyId.RetainAvailable, false);
        }

        public void WriteServerKeepAlive(ushort value)
        {
            if (value == 0)
            {
                return;
            }
            
            Write(MqttPropertyId.ServerKeepAlive, value);
        }

        public void WriteServerReference(string value)
        {
            Write(MqttPropertyId.ServerReference, value);
        }

        public void WriteSessionExpiryInterval(uint value)
        {
            // If the Session Expiry Interval is absent the value 0 is used.
            if (value == 0)
            {
                return;
            }
            
            WriteAsFourByteInteger(MqttPropertyId.SessionExpiryInterval, value);
        }

        public void WriteSharedSubscriptionAvailable(bool value)
        {
            if (value)
            {
                // Absence of the flag means it is supported! 
                return;
            }

            Write(MqttPropertyId.SharedSubscriptionAvailable, false);
        }

        public void WriteSubscriptionIdentifier(uint value)
        {
            WriteAsVariableLengthInteger(MqttPropertyId.SubscriptionIdentifier, value);
        }

        public void WriteSubscriptionIdentifiers(ICollection<uint> value)
        {
            if (value == null)
            {
                return;
            }

            foreach (var subscriptionIdentifier in value)
            {
                WriteAsVariableLengthInteger(MqttPropertyId.SubscriptionIdentifier, subscriptionIdentifier);
            }
        }

        public void WriteSubscriptionIdentifiersAvailable(bool value)
        {
            if (value)
            {
                // Absence of the flag means it is supported! 
                return;
            }

            Write(MqttPropertyId.SubscriptionIdentifiersAvailable, false);
        }

        public void WriteTo(IMqttPacketWriter packetWriter)
        {
            if (packetWriter == null)
            {
                throw new ArgumentNullException(nameof(packetWriter));
            }

            packetWriter.WriteVariableLengthInteger((uint)_packetWriter.Length);
            packetWriter.Write(_packetWriter);
        }

        public void WriteTopicAlias(ushort value)
        {
            // A Topic Alias of 0 is not permitted. A sender MUST NOT send a PUBLISH packet containing a Topic Alias which has the value 0.
            if (value == 0)
            {
                return;
            }
            
            Write(MqttPropertyId.TopicAlias, value);
        }

        public void WriteTopicAliasMaximum(ushort value)
        {
            // If the Topic Alias Maximum property is absent, the default value is 0.
            if (value == 0)
            {
                return;
            }

            Write(MqttPropertyId.TopicAliasMaximum, value);
        }

        public void WriteUserProperties(List<MqttUserProperty> userProperties)
        {
            if (userProperties == null || userProperties.Count == 0)
            {
                return;
            }

            foreach (var property in userProperties)
            {
                _packetWriter.Write((byte)MqttPropertyId.UserProperty);
                _packetWriter.WriteWithLengthPrefix(property.Name);
                _packetWriter.WriteWithLengthPrefix(property.Value);
            }
        }

        public void WriteWildcardSubscriptionAvailable(bool value)
        {
            // If not present, then Wildcard Subscriptions are supported.
            if (value)
            {
                return;
            }
            
            Write(MqttPropertyId.WildcardSubscriptionAvailable, false);
        }

        public void WriteWillDelayInterval(uint value)
        {
            // If the Will Delay Interval is absent, the default value is 0 and there is no delay before the Will Message is published.
            if (value == 0)
            {
                return;
            }
            
            WriteAsFourByteInteger(MqttPropertyId.WillDelayInterval, value);
        }

        void Write(MqttPropertyId id, bool value)
        {
            _packetWriter.Write((byte)id);
            _packetWriter.Write(value ? (byte)0x1 : (byte)0x0);
        }

        void Write(MqttPropertyId id, byte value)
        {
            _packetWriter.Write((byte)id);
            _packetWriter.Write(value);
        }

        void Write(MqttPropertyId id, ushort value)
        {
            _packetWriter.Write((byte)id);
            _packetWriter.Write(value);
        }

        void Write(MqttPropertyId id, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.WriteWithLengthPrefix(value);
        }

        void Write(MqttPropertyId id, byte[] value)
        {
            if (value == null)
            {
                return;
            }

            _packetWriter.Write((byte)id);
            _packetWriter.WriteWithLengthPrefix(value);
        }

        void WriteAsFourByteInteger(MqttPropertyId id, uint value)
        {
            _packetWriter.Write((byte)id);
            _packetWriter.Write((byte)(value >> 24));
            _packetWriter.Write((byte)(value >> 16));
            _packetWriter.Write((byte)(value >> 8));
            _packetWriter.Write((byte)value);
        }

        void WriteAsVariableLengthInteger(MqttPropertyId id, uint value)
        {
            _packetWriter.Write((byte)id);
            _packetWriter.WriteVariableLengthInteger(value);
        }
    }
}