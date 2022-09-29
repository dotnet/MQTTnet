// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Client
{
    public sealed class MqttClientDisconnectOptions
    {
        /// <summary>
        ///     Gets or sets the reason code.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public MqttClientDisconnectReason Reason { get; set; } = MqttClientDisconnectReason.NormalDisconnection;

        /// <summary>
        ///     Gets or sets the reason string.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public string ReasonString { get; set; }
    }
}