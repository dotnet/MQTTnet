// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Disconnecting;

namespace MQTTnet.Server.Formatter;

public static class MqttDisconnectPacketFactory
{
    static readonly MqttDisconnectPacket DefaultNormalDisconnection = new()
    {
        ReasonCode = MqttDisconnectReasonCode.NormalDisconnection,
        UserProperties = null,
        ReasonString = null,
        ServerReference = null,
        SessionExpiryInterval = 0
    };

    public static MqttDisconnectPacket Create(MqttServerClientDisconnectOptions clientDisconnectOptions)
    {
        if (clientDisconnectOptions == null)
        {
            return DefaultNormalDisconnection;
        }

        return new MqttDisconnectPacket
        {
            ReasonCode = clientDisconnectOptions.ReasonCode,
            UserProperties = clientDisconnectOptions.UserProperties,
            ReasonString = clientDisconnectOptions.ReasonString,
            ServerReference = clientDisconnectOptions.ServerReference,
            SessionExpiryInterval = 0 // TODO: Not yet supported!
        };
    }
}