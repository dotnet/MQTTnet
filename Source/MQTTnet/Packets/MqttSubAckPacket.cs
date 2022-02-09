// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttSubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        ///     Reason Code is used in MQTTv5.0.0 and backward compatible to v.3.1.1. Return Code is used in MQTTv3.1.1
        /// </summary>
        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>();

        /// <summary>
        ///     Added in MQTTv5.
        /// </summary>
        public string ReasonString { get; set; }

        /// <summary>
        ///     Added in MQTTv5.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }

        public override string ToString()
        {
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return $"SubAck: [PacketIdentifier={PacketIdentifier}] [ReasonCode={reasonCodesText}]";
        }
    }
}