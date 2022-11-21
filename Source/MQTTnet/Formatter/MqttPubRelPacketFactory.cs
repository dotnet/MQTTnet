// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubRelPacketFactory
    {
        public MqttPubRelPacket Create(MqttPubRecPacket pubRecPacket, MqttPubRelReasonCode reasonCode)
        {
            if (pubRecPacket == null)
            {
                throw new ArgumentNullException(nameof(pubRecPacket));
            }

            return new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier,
                ReasonCode = reasonCode
            };
        }

        public MqttPubRelPacket Create(MqttClientReleasePublishPacketOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new MqttPubRelPacket
            {
                PacketIdentifier = options.PacketIdentifier,
                ReasonCode = options.ReasonCode,
                ReasonString = options.ReasonString,
                UserProperties = options.UserProperties
            };
        }
    }
}