// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public sealed class MqttDisconnectPacketFactory
    {
        static readonly MqttDisconnectPacket DefaultNormalDisconnection = new MqttDisconnectPacket
        {
            ReasonCode = MqttDisconnectReasonCode.NormalDisconnection,
            UserProperties = null,
            ReasonString = null,
            ServerReference = null,
            SessionExpiryInterval = 0
        };

        static readonly MqttDisconnectPacket DefaultServerShuttingDown = new MqttDisconnectPacket
        {
            ReasonCode = MqttDisconnectReasonCode.ServerShuttingDown,
            UserProperties = null,
            ReasonString = null,
            ServerReference = null,
            SessionExpiryInterval = 0
        };

        static readonly MqttDisconnectPacket DefaultUnspecifiedError = new MqttDisconnectPacket
        {
            ReasonCode = MqttDisconnectReasonCode.UnspecifiedError,
            UserProperties = null,
            ReasonString = null,
            ServerReference = null,
            SessionExpiryInterval = 0
        };

        public MqttDisconnectPacket Create(MqttDisconnectReasonCode reasonCode)
        {
            if (reasonCode == MqttDisconnectReasonCode.NormalDisconnection)
            {
                return DefaultNormalDisconnection;
            }

            if (reasonCode == MqttDisconnectReasonCode.ServerShuttingDown)
            {
                return DefaultServerShuttingDown;
            }

            if (reasonCode == MqttDisconnectReasonCode.UnspecifiedError)
            {
                return DefaultUnspecifiedError;
            }

            return new MqttDisconnectPacket
            {
                ReasonCode = reasonCode,
                UserProperties = null,
                ReasonString = null,
                ServerReference = null,
                SessionExpiryInterval = 0
            };
        }

        public MqttDisconnectPacket Create(MqttClientDisconnectOptions clientDisconnectOptions)
        {
            if (clientDisconnectOptions == null)
            {
                return DefaultNormalDisconnection;
            }

            return new MqttDisconnectPacket
            {
                ReasonCode = (MqttDisconnectReasonCode)clientDisconnectOptions.Reason,
                UserProperties = clientDisconnectOptions.UserProperties,
                SessionExpiryInterval = clientDisconnectOptions.SessionExpiryInterval
            };
        }
    }
}