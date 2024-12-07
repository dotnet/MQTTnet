// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using MQTTnet.Formatter;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class ClientConnectedEventArgs : EventArgs
    {
        readonly MqttConnectPacket _connectPacket;

        public ClientConnectedEventArgs(MqttConnectPacket connectPacket, MqttProtocolVersion protocolVersion, EndPoint remoteEndPoint, IDictionary sessionItems)
        {
            _connectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));
            ProtocolVersion = protocolVersion;
            RemoteEndPoint = remoteEndPoint;
            SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        public ReadOnlyMemory<byte> AuthenticationData => _connectPacket.AuthenticationData;

        public string AuthenticationMethod => _connectPacket.AuthenticationMethod;

        /// <summary>
        ///     Gets the client identifier of the connected client.
        ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId => _connectPacket.ClientId;

        /// <summary>
        ///     Gets the endpoint of the connected client.
        /// </summary>
        public EndPoint RemoteEndPoint { get; }

        [Obsolete("Use RemoteEndPoint instead.")]
        public string Endpoint => RemoteEndPoint?.ToString();

        /// <summary>
        ///     Gets the protocol version which is used by the connected client.
        /// </summary>
        public MqttProtocolVersion ProtocolVersion { get; }

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems { get; }

        /// <summary>
        ///     Gets the user name of the connected client.
        /// </summary>
        public string UserName => _connectPacket.Username;

        /// <summary>
        ///     Gets the user properties sent by the client.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties => _connectPacket?.UserProperties;
    }
}