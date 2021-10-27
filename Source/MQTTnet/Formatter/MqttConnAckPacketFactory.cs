using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnAckPacketFactory
    {
        public MqttConnAckPacket Create(ValidatingMqttClientConnectionEventArgs clientConnectionValidationEventArgs)
        {
            if (clientConnectionValidationEventArgs == null) throw new ArgumentNullException(nameof(clientConnectionValidationEventArgs));

            var connAckPacket = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReasonCodeConverter.ToConnectReturnCode(clientConnectionValidationEventArgs.ReasonCode),
                ReasonCode = clientConnectionValidationEventArgs.ReasonCode,
                Properties =
                {
                    RetainAvailable = true,
                    SubscriptionIdentifiersAvailable = true,
                    SharedSubscriptionAvailable = false,
                    TopicAliasMaximum = ushort.MaxValue,
                    WildcardSubscriptionAvailable = true,
                    
                    AuthenticationMethod = clientConnectionValidationEventArgs.AuthenticationMethod,
                    AuthenticationData = clientConnectionValidationEventArgs.ResponseAuthenticationData,
                    AssignedClientIdentifier = clientConnectionValidationEventArgs.AssignedClientIdentifier,
                    ReasonString = clientConnectionValidationEventArgs.ReasonString,
                    ServerReference = clientConnectionValidationEventArgs.ServerReference
                }
            };

            if (clientConnectionValidationEventArgs.ResponseUserProperties != null)
            {
                connAckPacket.Properties.UserProperties.AddRange(clientConnectionValidationEventArgs.ResponseUserProperties);
            }
            
            return connAckPacket;
        }
    }
}