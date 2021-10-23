using System;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client.Connecting
{
    public sealed class MqttClientConnectResultFactory
    {
        public MqttClientConnectResult Create(MqttConnAckPacket connAckPacket, MqttProtocolVersion protocolVersion)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            if (protocolVersion == MqttProtocolVersion.V500)
            {
                return CreateForMqtt500(connAckPacket);
            }

            return CreateForMqtt311(connAckPacket);
        }
        
        static MqttClientConnectResult CreateForMqtt500(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            return new MqttClientConnectResult
            {
                IsSessionPresent = connAckPacket.IsSessionPresent,
                ResultCode = (MqttClientConnectResultCode) (int) connAckPacket.ReasonCode,
                WildcardSubscriptionAvailable = connAckPacket.Properties.WildcardSubscriptionAvailable,
                RetainAvailable = connAckPacket.Properties.RetainAvailable,
                AssignedClientIdentifier = connAckPacket.Properties.AssignedClientIdentifier,
                AuthenticationMethod = connAckPacket.Properties.AuthenticationMethod,
                AuthenticationData = connAckPacket.Properties.AuthenticationData,
                MaximumPacketSize = connAckPacket.Properties.MaximumPacketSize,
                ReasonString = connAckPacket.Properties.ReasonString,
                ReceiveMaximum = connAckPacket.Properties.ReceiveMaximum,
                MaximumQoS = connAckPacket.Properties.MaximumQoS ?? MqttQualityOfServiceLevel.ExactlyOnce,
                ResponseInformation = connAckPacket.Properties.ResponseInformation,
                TopicAliasMaximum = connAckPacket.Properties.TopicAliasMaximum,
                ServerReference = connAckPacket.Properties.ServerReference,
                ServerKeepAlive = connAckPacket.Properties.ServerKeepAlive,
                SessionExpiryInterval = connAckPacket.Properties?.SessionExpiryInterval,
                SubscriptionIdentifiersAvailable = connAckPacket.Properties.SubscriptionIdentifiersAvailable,
                SharedSubscriptionAvailable = connAckPacket.Properties.SharedSubscriptionAvailable,
                UserProperties = connAckPacket.Properties.UserProperties 
            };
        }
        
        static MqttClientConnectResult CreateForMqtt311(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            return new MqttClientConnectResult
            {
                RetainAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
                WildcardSubscriptionAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
                IsSessionPresent = connAckPacket.IsSessionPresent,
                ResultCode = ConvertReturnCodeToResultCode(connAckPacket.ReturnCode)
            };
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
    }
}