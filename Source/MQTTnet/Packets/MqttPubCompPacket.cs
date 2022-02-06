// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubCompPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }
        
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubCompReasonCode ReasonCode { get; set; } = MqttPubCompReasonCode.Success;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubCompPacketProperties Properties { get; } = new MqttPubCompPacketProperties();
        
        public override string ToString()
        {
            return string.Concat("PubComp: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
