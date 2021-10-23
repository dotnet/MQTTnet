using MQTTnet.Exceptions;
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

                case MqttConnectReasonCode.NotAuthorized:
                    {
                        return MqttConnectReturnCode.ConnectionRefusedNotAuthorized;
                    }

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

                case MqttConnectReasonCode.ServerUnavailable:
                case MqttConnectReasonCode.ServerBusy:
                case MqttConnectReasonCode.ServerMoved:
                    {
                        return MqttConnectReturnCode.ConnectionRefusedServerUnavailable;
                    }

                default:
                    {
                        throw new MqttProtocolViolationException("Unable to convert connect reason code (MQTTv5) to return code (MQTTv3).");
                    }
            }
        }

        public static MqttConnectReasonCode ToConnectReasonCode(MqttConnectReturnCode returnCode)
        {
            switch (returnCode)
            {
                case MqttConnectReturnCode.ConnectionAccepted:
                    {
                        return MqttConnectReasonCode.Success;
                    }

                case MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion:
                    {
                        return MqttConnectReasonCode.UnsupportedProtocolVersion;
                    }

                case MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword:
                    {
                        return MqttConnectReasonCode.BadUserNameOrPassword;
                    }

                case MqttConnectReturnCode.ConnectionRefusedIdentifierRejected:
                    {
                        return MqttConnectReasonCode.ClientIdentifierNotValid;
                    }

                case MqttConnectReturnCode.ConnectionRefusedServerUnavailable:
                    {
                        return MqttConnectReasonCode.ServerUnavailable;
                    }

                case MqttConnectReturnCode.ConnectionRefusedNotAuthorized:
                    {
                        return MqttConnectReasonCode.NotAuthorized;
                    }

                default:
                    {
                        throw new MqttProtocolViolationException("Unable to convert connect reason code (MQTTv5) to return code (MQTTv3).");
                    }
            }
        }
    }
}
