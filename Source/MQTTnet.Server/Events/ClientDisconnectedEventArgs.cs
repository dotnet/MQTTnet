// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class ClientDisconnectedEventArgs : EventArgs
    {
        readonly MqttDisconnectPacket _disconnectPacket;

        public ClientDisconnectedEventArgs(
            string clientId,
            MqttDisconnectPacket disconnectPacket,
            MqttClientDisconnectType disconnectType,
            string endpoint,
            IDictionary sessionItems)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            DisconnectType = disconnectType;
            Endpoint = endpoint;
            SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));

            // The DISCONNECT packet can be null in case of a non clean disconnect or session takeover.
            _disconnectPacket = disconnectPacket;
        }

        /// <summary>
        ///     Gets the client identifier.
        ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        public MqttClientDisconnectType DisconnectType { get; }

        public string Endpoint { get; }

        /// <summary>
        ///     Gets the reason code sent by the client.
        ///     Only available for clean disconnects.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public MqttDisconnectReasonCode? ReasonCode => _disconnectPacket?.ReasonCode;

        /// <summary>
        ///     Gets the reason string sent by the client.
        ///     Only available for clean disconnects.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public string ReasonString => _disconnectPacket?.ReasonString;

        /// <summary>
        ///     Gets the session expiry interval sent by the client.
        ///     Only available for clean disconnects.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public uint SessionExpiryInterval => _disconnectPacket?.SessionExpiryInterval ?? 0;

        /// <summary>
        ///     Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary SessionItems { get; }

        /// <summary>
        ///     Gets the user properties sent by the client.
        ///     Only available for clean disconnects.
        ///     <remarks>MQTT 5.0.0+ feature.</remarks>
        /// </summary>
        public List<MqttUserProperty> UserProperties => _disconnectPacket?.UserProperties;
    }
}