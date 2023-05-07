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
        public MqttAuthPacket CreateReAuthenticationPacket(MqttClientOptions clientOptions, MqttReAuthenticationOptions reAuthenticationOptions)
        {
            if (clientOptions == null)
            {
                throw new ArgumentNullException(nameof(clientOptions));
            }

            if (reAuthenticationOptions == null)
            {
                throw new ArgumentNullException(nameof(reAuthenticationOptions));
            }

            return new MqttAuthPacket
            {
                // The authentication method cannot change and must be always the same as long as the client is connected.
                AuthenticationMethod = clientOptions.AuthenticationMethod,
                ReasonCode = MqttAuthenticateReasonCode.ReAuthenticate,
                AuthenticationData = reAuthenticationOptions.AuthenticationData,
                UserProperties = reAuthenticationOptions.UserProperties,
                ReasonString = null
            };
        }
    }
}