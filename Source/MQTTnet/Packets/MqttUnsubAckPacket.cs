// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubAckPacket : MqttPacketWithIdentifier
    {
        /// <summary>
        ///     Added in MQTTv5.
        /// </summary>
        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; set; }

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
            var reasonCodesText = string.Empty;
            if (ReasonCodes != null)
            {
                reasonCodesText = string.Join(",", ReasonCodes?.Select(f => f.ToString()));
            }

            return $"UnsubAck: [PacketIdentifier={PacketIdentifier}] [ReasonCodes={reasonCodesText}] [ReasonString={ReasonString}]";
        }
    }
}