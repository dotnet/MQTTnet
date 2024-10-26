// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet;

public sealed class MqttClientConnectResultFactory
{
    public MqttClientConnectResult Create(MqttConnAckPacket connAckPacket, MqttProtocolVersion protocolVersion)
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
        switch (connectReturnCode)
        {
            case MqttConnectReturnCode.ConnectionAccepted:
            {
                return MqttClientConnectResultCode.Success;
            }

            case MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion:
            {
                return MqttClientConnectResultCode.UnsupportedProtocolVersion;
            }

            case MqttConnectReturnCode.ConnectionRefusedNotAuthorized:
            {
                return MqttClientConnectResultCode.NotAuthorized;
            }

            case MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword:
            {
                return MqttClientConnectResultCode.BadUserNameOrPassword;
            }

            case MqttConnectReturnCode.ConnectionRefusedIdentifierRejected:
            {
                return MqttClientConnectResultCode.ClientIdentifierNotValid;
            }

            case MqttConnectReturnCode.ConnectionRefusedServerUnavailable:
            {
                return MqttClientConnectResultCode.ServerUnavailable;
            }

            default:
                throw new MqttProtocolViolationException("Received unexpected return code.");
        }
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
            AuthenticationData = connAckPacket.AuthenticationData,
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