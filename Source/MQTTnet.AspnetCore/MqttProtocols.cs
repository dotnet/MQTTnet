// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.AspNetCore
{
    public enum MqttProtocols
    {
        /// <summary>
        /// Only support Mqtt
        /// </summary>
        Mqtt,

        /// <summary>
        /// Only support Mqtt-over-WebSocket        
        /// </summary>
        WebSocket,

        /// <summary>
        /// Support both Mqtt and Mqtt-over-WebSocket     
        /// </summary>
        MqttAndWebSocket
    }
}
