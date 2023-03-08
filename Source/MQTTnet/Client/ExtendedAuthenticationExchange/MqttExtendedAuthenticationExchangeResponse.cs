// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttExtendedAuthenticationExchangeResponse
    {
        /// <summary>
        ///     Gets or sets the authentication data.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public byte[] AuthenticationData { get; set; }

        /// <summary>
        ///     Gets or sets the user properties which will be sent to the server.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; } = new List<MqttUserProperty>();
    }
}