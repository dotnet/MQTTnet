// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubCompPacketFactory
    {
        public MqttPubCompPacket Create(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRelPacket == null)
            {
                throw new ArgumentNullException(nameof(pubRelPacket));
            }

            return new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = (MqttPubCompReasonCode)(int)reasonCode
            };
        }
    }
}