// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public static class MqttClientConnectResultFactory
{
    public static MqttClientConnectResult Create(MqttConnAckPacket connAckPacket, MqttProtocolVersion protocolVersion)
    {
        ArgumentNullException.ThrowIfNull(connAckPacket);

        if (protocolVersion == MqttProtocolVersion.V500)
        {
            return CreateForMqtt500(connAckPacket);
        }

        return CreateForMqtt311(connAckPacket);
    }

    static MqttClientConnectResultCode ConvertReturnCodeToResultCode(MqttConnectReturnCode connectReturnCode)
    {
        return connectReturnCode switch
        {
            MqttConnectReturnCode.ConnectionAccepted => MqttClientConnectResultCode.Success,
            MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion => MqttClientConnectResultCode.UnsupportedProtocolVersion,
            MqttConnectReturnCode.ConnectionRefusedNotAuthorized => MqttClientConnectResultCode.NotAuthorized,
            MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword => MqttClientConnectResultCode.BadUserNameOrPassword,
            MqttConnectReturnCode.ConnectionRefusedIdentifierRejected => MqttClientConnectResultCode.ClientIdentifierNotValid,
            MqttConnectReturnCode.ConnectionRefusedServerUnavailable => MqttClientConnectResultCode.ServerUnavailable,
            _ => throw new MqttProtocolViolationException("Received unexpected return code.")
        };
    }

    static MqttClientConnectResult CreateForMqtt311(MqttConnAckPacket connAckPacket)
    {
        ArgumentNullException.ThrowIfNull(connAckPacket);


        return new MqttClientConnectResult
        {
            RetainAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
            WildcardSubscriptionAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
            IsSessionPresent = connAckPacket.IsSessionPresent,
            ResultCode = ConvertReturnCodeToResultCode(connAckPacket.ReturnCode)
        };
    }

    static MqttClientConnectResult CreateForMqtt500(MqttConnAckPacket connAckPacket)
    {
        ArgumentNullException.ThrowIfNull(connAckPacket);

        return new MqttClientConnectResult
        {
            IsSessionPresent = connAckPacket.IsSessionPresent,
            ResultCode = (MqttClientConnectResultCode)(int)connAckPacket.ReasonCode,
            WildcardSubscriptionAvailable = connAckPacket.WildcardSubscriptionAvailable,
            RetainAvailable = connAckPacket.RetainAvailable,
            AssignedClientIdentifier = connAckPacket.AssignedClientIdentifier,
            AuthenticationMethod = connAckPacket.AuthenticationMethod,
            AuthenticationData = connAckPacket.AuthenticationData.ToArray(),
            MaximumPacketSize = connAckPacket.MaximumPacketSize,
            ReasonString = connAckPacket.ReasonString,
            ReceiveMaximum = connAckPacket.ReceiveMaximum,
            MaximumQoS = connAckPacket.MaximumQoS,
            ResponseInformation = connAckPacket.ResponseInformation,
            TopicAliasMaximum = connAckPacket.TopicAliasMaximum,
            ServerReference = connAckPacket.ServerReference,
            ServerKeepAlive = connAckPacket.ServerKeepAlive,
            SessionExpiryInterval = connAckPacket.SessionExpiryInterval,
            SubscriptionIdentifiersAvailable = connAckPacket.SubscriptionIdentifiersAvailable,
            SharedSubscriptionAvailable = connAckPacket.SharedSubscriptionAvailable,
            UserProperties = connAckPacket.UserProperties
        };
    }
}