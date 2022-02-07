// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientPublishResultFactory
    {
        static readonly IReadOnlyCollection<MqttUserProperty> EmptyUserProperties = new List<MqttUserProperty>();

        public MqttClientPublishResult Create(MqttPubAckPacket pubAckPacket)
        {
            var result = new MqttClientPublishResult
            {
                ReasonCode = MqttClientPublishReasonCode.Success
            };

            if (pubAckPacket != null)
            {
                // QoS 0 has no response. So we treat it as a success always.
                // Both enums have the same values. So it can be easily converted.
                result.ReasonCode = (MqttClientPublishReasonCode) (int) pubAckPacket.ReasonCode;
                result.PacketIdentifier = pubAckPacket.PacketIdentifier;
                result.ReasonString = pubAckPacket.ReasonString;
                result.UserProperties = pubAckPacket.UserProperties ?? EmptyUserProperties;
            }

            return result;
        }

        public MqttClientPublishResult Create(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
        {
            if (pubRecPacket == null || pubCompPacket == null)
            {
                return new MqttClientPublishResult
                {
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError
                };
            }

            MqttClientPublishResult result;
            
            // The PUBCOMP is the last packet in QoS 2. So we use the results from that instead of PUBREC.
            if (pubCompPacket.ReasonCode == MqttPubCompReasonCode.PacketIdentifierNotFound)
            {
                result = new MqttClientPublishResult
                {
                    PacketIdentifier = pubCompPacket.PacketIdentifier,
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError,
                    ReasonString = pubCompPacket.ReasonString,
                    UserProperties = pubCompPacket.UserProperties ?? EmptyUserProperties
                };
                
                return result;
            }

            result = new MqttClientPublishResult
            {
                PacketIdentifier = pubCompPacket.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success,
                ReasonString = pubCompPacket.ReasonString,
                UserProperties = pubCompPacket.UserProperties ?? EmptyUserProperties
            };

            if (pubRecPacket.ReasonCode != MqttPubRecReasonCode.Success)
            {
                // Both enums share the same values.
                result.ReasonCode = (MqttClientPublishReasonCode) pubRecPacket.ReasonCode;
            }

            return result;
        }
    }
}