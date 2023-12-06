// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;
using MQTTnet.Server.Disconnecting;

namespace MQTTnet.Server
{
    public sealed class MqttServerStopOptions
    {
        /// <summary>
        ///     These disconnect options are sent to every connected client via a DISCONNECT packet.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttServerClientDisconnectOptions DefaultClientDisconnectOptions { get; set; } = new MqttServerClientDisconnectOptions
        {
            ReasonCode = MqttDisconnectReasonCode.ServerShuttingDown,
            UserProperties = null,
            ReasonString = null,
            ServerReference = null
        };
    }
}