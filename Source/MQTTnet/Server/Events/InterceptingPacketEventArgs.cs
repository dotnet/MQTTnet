// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class InterceptingPacketEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the client ID which has sent the packet or will receive the packet.
        /// </summary>
        public string ClientId { get; internal set; }
        
        /// <summary>
        /// Gets the endpoint of the sending or receiving client.
        /// </summary>
        public string Endpoint { get; internal set; }
        
        /// <summary>
        /// Gets or sets the MQTT packet which was received or will be sent.
        /// </summary>
        public MqttPacket Packet { get; set; }

        /// <summary>
        /// Gets or sets whether the packet should be processed or not.
        /// </summary>
        public bool ProcessPacket { get; set; } = true;
        
        /// <summary>
        /// Gets the cancellation token from the connection managing thread.
        /// Use this in further event processing.
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }
    }
}