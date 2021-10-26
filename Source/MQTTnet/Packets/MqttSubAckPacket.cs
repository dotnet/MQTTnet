﻿using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttSubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }
        
        /// <summary>
        /// Reason Code is used in MQTTv5.0.0 and backward compatible to v.3.1.1.
        /// Return Code is used in MQTTv3.1.1
        /// </summary>
        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>();

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttSubAckPacketProperties Properties { get; } = new MqttSubAckPacketProperties();

        public override string ToString()
        {
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("SubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", reasonCodesText, "]");
        }
    }
}
