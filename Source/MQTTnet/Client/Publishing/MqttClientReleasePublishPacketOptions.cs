// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClientReleasePublishPacketOptions
    {
        /// <summary>
        ///     Gets or sets the identifier of the PUBLISH packet which should be released.
        /// </summary>
        public ushort PacketIdentifier { get; set; }
        
        public MqttPubRelReasonCode ReasonCode { get; set; } = MqttPubRelReasonCode.Success;

        /// <summary>
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public string ReasonString { get; set; }

        /// <summary>
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}