// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public sealed class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(string clientId, string userName, MqttProtocolVersion protocolVersion, string endpoint, IDictionary sessionItems)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            UserName = userName;
            ProtocolVersion = protocolVersion;
            Endpoint = endpoint;
            SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
        }

        /// <summary>
        ///     Gets the client identifier of the connected client.
        ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        ///     Gets the endpoint of the connected client.
        /// </summary>
        public string Endpoint { get; }

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
        public string UserName { get; }
    }
}