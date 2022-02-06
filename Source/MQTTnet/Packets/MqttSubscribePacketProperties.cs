// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttSubscribePacketProperties
    {
        /// <summary>
        /// It is a Protocol Error if the Subscription Identifier has a value of 0.
        /// </summary>
        public uint SubscriptionIdentifier { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
    }
}
