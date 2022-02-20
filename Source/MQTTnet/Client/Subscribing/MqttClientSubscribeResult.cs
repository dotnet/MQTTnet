// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientSubscribeResult
    {
        public IReadOnlyCollection<MqttClientSubscribeResultItem> Items { get; internal set; }
        
        /// <summary>
        /// Gets the user properties which were part of the SUBACK packet.
        /// MQTTv5 only.
        /// </summary>
        public IReadOnlyCollection<MqttUserProperty> UserProperties { get; internal set; }
        
        /// <summary>
        /// Gets the reason string.
        /// MQTTv5 only.
        /// </summary>
        public string ReasonString { get; internal set; }

        /// <summary>
        /// Gets the packet identifier which was used.
        /// </summary>
        public ushort PacketIdentifier { get; internal set; }
    }
}