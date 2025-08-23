// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using System;

namespace MQTTnet.Server
{
    public static class MqttExtendedAuthenticationExchangeResultFactory
    {
        public static MqttExtendedAuthenticationExchangeResult Create(MqttAuthPacket authPacket)
        {
            if (authPacket == null)
            {
                throw new ArgumentNullException(nameof(authPacket));
            }

            return new MqttExtendedAuthenticationExchangeResult
            {
                AuthenticationData = authPacket.AuthenticationData,

                ReasonString = authPacket.ReasonString,
                UserProperties = authPacket.UserProperties
            };
        }

        public static MqttExtendedAuthenticationExchangeResult Create(MqttDisconnectPacket disconnectPacket)
        {
            if (disconnectPacket == null)
            {
                throw new ArgumentNullException(nameof(disconnectPacket));
            }

            return new MqttExtendedAuthenticationExchangeResult
            {
                AuthenticationData = null,
                ReasonString = disconnectPacket.ReasonString,
                UserProperties = disconnectPacket.UserProperties

                // SessionExpiryInterval makes no sense because the connection is not yet made!
                // ServerReferences makes no sense when the client initiated a DISCONNECT!
            };
        }
    }
}
