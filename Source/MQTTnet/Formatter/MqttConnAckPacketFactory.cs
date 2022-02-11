// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnAckPacketFactory
    {
        public MqttConnAckPacket Create(ValidatingConnectionEventArgs validatingConnectionEventArgs)
        {
            if (validatingConnectionEventArgs == null) throw new ArgumentNullException(nameof(validatingConnectionEventArgs));

            var connAckPacket = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReasonCodeConverter.ToConnectReturnCode(validatingConnectionEventArgs.ReasonCode),
                ReasonCode = validatingConnectionEventArgs.ReasonCode,
                RetainAvailable = true,
                SubscriptionIdentifiersAvailable = true,
                SharedSubscriptionAvailable = false,
                TopicAliasMaximum = ushort.MaxValue,
                WildcardSubscriptionAvailable = true,
                    
                AuthenticationMethod = validatingConnectionEventArgs.AuthenticationMethod,
                AuthenticationData = validatingConnectionEventArgs.ResponseAuthenticationData,
                AssignedClientIdentifier = validatingConnectionEventArgs.AssignedClientIdentifier,
                ReasonString = validatingConnectionEventArgs.ReasonString,
                ServerReference = validatingConnectionEventArgs.ServerReference,
                UserProperties = validatingConnectionEventArgs.ResponseUserProperties
            };
            
            return connAckPacket;
        }
    }
}