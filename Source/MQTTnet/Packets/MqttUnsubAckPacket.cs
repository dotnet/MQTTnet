// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public MqttUnsubAckPacketProperties Properties { get; } = new MqttUnsubAckPacketProperties();

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; } = new List<MqttUnsubscribeReasonCode>();

        public override string ToString()
        {
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("UnsubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCodes=", reasonCodesText, "]");
        }
    }
}
