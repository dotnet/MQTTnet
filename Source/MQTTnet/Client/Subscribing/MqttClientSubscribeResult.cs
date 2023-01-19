// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientSubscribeResult
    {
        public MqttClientSubscribeResult(
            ushort packetIdentifier,
            IReadOnlyCollection<MqttClientSubscribeResultItem> items,
            string reasonString,
            IReadOnlyCollection<MqttUserProperty> userProperties)
        {
            PacketIdentifier = packetIdentifier;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            ReasonString = reasonString;
            UserProperties = userProperties ?? throw new ArgumentNullException(nameof(userProperties));
        }

        /// <summary>
        ///     Gets the result for every topic filter item.
        /// </summary>
        public IReadOnlyCollection<MqttClientSubscribeResultItem> Items { get; }

        /// <summary>
        ///     Gets the user properties which were part of the SUBACK packet.
        ///     MQTTv5 only.
        /// </summary>
        public ushort PacketIdentifier { get; }

        /// <summary>
        ///     Gets the reason string.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public string ReasonString { get; }

        /// <summary>
        ///     Gets the user properties which were part of the SUBACK packet.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public IReadOnlyCollection<MqttUserProperty> UserProperties { get; }
    }
}