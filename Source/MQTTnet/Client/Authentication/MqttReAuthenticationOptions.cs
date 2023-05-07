// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttReAuthenticationOptions
    {
        /// <summary>
        ///     Gets or sets the authentication data.
        ///     Authentication data is binary information used to transmit multiple iterations of cryptographic secrets of protocol
        ///     steps.
        ///     The content of the authentication data is highly dependent on the specific implementation of the authentication
        ///     method.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public byte[] AuthenticationData { get; set; }

        /// <summary>
        ///     Gets or sets the user properties.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}