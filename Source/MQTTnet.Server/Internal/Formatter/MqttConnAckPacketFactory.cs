// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal.Formatter;

public static class MqttConnAckPacketFactory
{
    public static MqttConnAckPacket Create(ValidatingConnectionEventArgs validatingConnectionEventArgs)
    {
        ArgumentNullException.ThrowIfNull(validatingConnectionEventArgs);

        var connAckPacket = new MqttConnAckPacket
        {
            ReturnCode = MqttConnectReasonCodeConverter.ToConnectReturnCode(validatingConnectionEventArgs.ReasonCode),
            ReasonCode = validatingConnectionEventArgs.ReasonCode,
            RetainAvailable = true,
            SubscriptionIdentifiersAvailable = true,
            SharedSubscriptionAvailable = false,
            TopicAliasMaximum = ushort.MaxValue,
            MaximumQoS = MqttQualityOfServiceLevel.ExactlyOnce,
            WildcardSubscriptionAvailable = true,

            AuthenticationMethod = validatingConnectionEventArgs.AuthenticationMethod,
            AuthenticationData = validatingConnectionEventArgs.ResponseAuthenticationData,
            AssignedClientIdentifier = validatingConnectionEventArgs.AssignedClientIdentifier,
            ReasonString = validatingConnectionEventArgs.ReasonString,
            ServerReference = validatingConnectionEventArgs.ServerReference,
            UserProperties = validatingConnectionEventArgs.ResponseUserProperties,

            ResponseInformation = null,
            MaximumPacketSize = 0, // Unlimited,
            ReceiveMaximum = 0 // Unlimited
        };

        return connAckPacket;
    }
}