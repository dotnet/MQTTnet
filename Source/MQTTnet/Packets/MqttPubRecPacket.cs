// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubRecPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        // MQTTv5+
        public MqttPubRecReasonCode ReasonCode { get; set; } = MqttPubRecReasonCode.Success;

        // MQTTv5+
        public string ReasonString { get; set; }

        // MQTTv5+
        public List<MqttUserProperty> UserProperties { get; set; }

        public override string ToString()
        {
            return string.Concat("PubRec: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}