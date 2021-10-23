using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnAckPacketFactory
    {
        public MqttConnAckPacket Create(MqttConnectionValidatorContext connectionValidatorContext)
        {
            if (connectionValidatorContext == null) throw new ArgumentNullException(nameof(connectionValidatorContext));

            var connAckPacket = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReasonCodeConverter.ToConnectReturnCode(connectionValidatorContext.ReasonCode),
                ReasonCode = connectionValidatorContext.ReasonCode,
                Properties =
                {
                    RetainAvailable = true,
                    SubscriptionIdentifiersAvailable = true,
                    SharedSubscriptionAvailable = false,
                    TopicAliasMaximum = ushort.MaxValue,
                    WildcardSubscriptionAvailable = true,
                    
                    AuthenticationMethod = connectionValidatorContext.AuthenticationMethod,
                    AuthenticationData = connectionValidatorContext.ResponseAuthenticationData,
                    AssignedClientIdentifier = connectionValidatorContext.AssignedClientIdentifier,
                    ReasonString = connectionValidatorContext.ReasonString,
                    ServerReference = connectionValidatorContext.ServerReference
                }
            };

            if (connectionValidatorContext.ResponseUserProperties != null)
            {
                connAckPacket.Properties.UserProperties.AddRange(connectionValidatorContext.ResponseUserProperties);
            }
            
            return connAckPacket;
        }
    }
}