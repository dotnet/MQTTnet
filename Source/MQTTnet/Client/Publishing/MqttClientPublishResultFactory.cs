// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientPublishResultFactory
    {
        static readonly IReadOnlyCollection<MqttUserProperty> EmptyUserProperties = new List<MqttUserProperty>();
        static readonly MqttClientPublishResult EmptySuccessResult = new MqttClientPublishResult(null, MqttClientPublishReasonCode.Success, null, EmptyUserProperties);

        public MqttClientPublishResult Create(MqttPubAckPacket pubAckPacket)
        {
            // QoS 0 has no response. So we treat it as a success always.
            if (pubAckPacket == null)
            {
                return EmptySuccessResult;
            }

            var result = new MqttClientPublishResult(
                pubAckPacket.PacketIdentifier,
                (MqttClientPublishReasonCode)(int)pubAckPacket.ReasonCode,
                pubAckPacket.ReasonString,
                pubAckPacket.UserProperties ?? EmptyUserProperties);

            return result;
        }

        public MqttClientPublishResult Create(MqttPubRecPacket pubRecPacket)
        {
            if (pubRecPacket == null)
            {
                throw new ArgumentNullException(nameof(pubRecPacket));
            }

            return new MqttClientPublishResult(
                pubRecPacket.PacketIdentifier,
                (MqttClientPublishReasonCode)(int)pubRecPacket.ReasonCode,
                pubRecPacket.ReasonString,
                pubRecPacket.UserProperties ?? EmptyUserProperties);
        }

        public MqttClientPublishResult Create(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
        {
            if (pubRecPacket == null || pubCompPacket == null)
            {
                return new MqttClientPublishResult(null, MqttClientPublishReasonCode.UnspecifiedError, null, EmptyUserProperties);
            }

            // The PUBCOMP is the last packet in QoS 2. So we use the results from that instead of PUBREC.
            if (pubCompPacket.ReasonCode == MqttPubCompReasonCode.PacketIdentifierNotFound)
            {
                return new MqttClientPublishResult(
                    pubCompPacket.PacketIdentifier,
                    MqttClientPublishReasonCode.UnspecifiedError,
                    pubCompPacket.ReasonString,
                    pubCompPacket.UserProperties ?? EmptyUserProperties);
            }

            var reasonCode = MqttClientPublishReasonCode.Success;

            if (pubRecPacket.ReasonCode != MqttPubRecReasonCode.Success)
            {
                // Both enums share the same values.
                reasonCode = (MqttClientPublishReasonCode)pubRecPacket.ReasonCode;
            }

            return new MqttClientPublishResult(pubCompPacket.PacketIdentifier, reasonCode, pubCompPacket.ReasonString, pubCompPacket.UserProperties ?? EmptyUserProperties);
        }
    }
}