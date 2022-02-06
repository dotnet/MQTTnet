// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttDisconnectPacket : MqttBasePacket
    {
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttDisconnectPacketProperties Properties { get; } = new MqttDisconnectPacketProperties();

        public override string ToString()
        {
            return string.Concat("Disconnect: [ReasonCode=", ReasonCode, "]");
        }
    }
}
