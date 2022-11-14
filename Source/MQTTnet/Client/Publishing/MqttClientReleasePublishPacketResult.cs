// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientReleasePublishPacketResult
    {
        public MqttClientReleasePublishPacketResult(uint packetIdentifier, string reasonString, List<MqttUserProperty> userProperties)
        {
            PacketIdentifier = packetIdentifier;
            ReasonString = reasonString;
            UserProperties = userProperties;
        }

        /// <summary>
        ///     Gets or sets the identifier which was used in the PUBCOMP packet.
        /// </summary>
        public uint PacketIdentifier { get; }

        public string ReasonString { get; }

        /// <summary>
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; }
    }
}