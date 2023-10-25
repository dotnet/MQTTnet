// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Formatter
{
    public static class MqttConnectReasonCodeConverter
    {
        public static MqttConnectReturnCode ToConnectReturnCode(MqttConnectReasonCode reasonCode)
        {
            switch (reasonCode)
            {
                case MqttConnectReasonCode.Success:
                {
                    return MqttConnectReturnCode.ConnectionAccepted;
                }

                case MqttConnectReasonCode.Banned:
                case MqttConnectReasonCode.NotAuthorized:
                {
                    return MqttConnectReturnCode.ConnectionRefusedNotAuthorized;
                }

                case MqttConnectReasonCode.BadAuthenticationMethod:
                case MqttConnectReasonCode.BadUserNameOrPassword:
                {
                    return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                }

                case MqttConnectReasonCode.ClientIdentifierNotValid:
                {
                    return MqttConnectReturnCode.ConnectionRefusedIdentifierRejected;
                }

                case MqttConnectReasonCode.UnsupportedProtocolVersion:
                {
                    return MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion;
                }

                case MqttConnectReasonCode.UseAnotherServer:
                case MqttConnectReasonCode.ServerUnavailable:
                case MqttConnectReasonCode.ServerBusy:
                case MqttConnectReasonCode.ServerMoved:
                {
                    return MqttConnectReturnCode.ConnectionRefusedServerUnavailable;
                }

                default:
                    return MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion;
            }
        }
    }
}