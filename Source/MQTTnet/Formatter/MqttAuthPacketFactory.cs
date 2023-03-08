// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttAuthPacketFactory
    {
        public MqttAuthPacket Create(MqttAuthPacket serverAuthPacket, MqttExtendedAuthenticationExchangeResponse response)
        {
            if (serverAuthPacket == null)
            {
                throw new ArgumentNullException(nameof(serverAuthPacket));
            }

            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return new MqttAuthPacket
            {
                ReasonCode = MqttAuthenticateReasonCode.ContinueAuthentication,
                ReasonString = null,
                AuthenticationMethod = serverAuthPacket.AuthenticationMethod,
                AuthenticationData = response.AuthenticationData,
                UserProperties = response.UserProperties
            };
        }
    }
}