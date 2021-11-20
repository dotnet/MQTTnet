using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnAckPacketFactory
    {
        public MqttConnAckPacket Create(ValidatingConnectionEventArgs validatingConnectionEventArgs)
        {
            if (validatingConnectionEventArgs == null) throw new ArgumentNullException(nameof(validatingConnectionEventArgs));

            var connAckPacket = new MqttConnAckPacket
            {
                ReturnCode = MqttConnectReasonCodeConverter.ToConnectReturnCode(validatingConnectionEventArgs.ReasonCode),
                ReasonCode = validatingConnectionEventArgs.ReasonCode,
                Properties =
                {
                    RetainAvailable = true,
                    SubscriptionIdentifiersAvailable = true,
                    SharedSubscriptionAvailable = false,
                    TopicAliasMaximum = ushort.MaxValue,
                    WildcardSubscriptionAvailable = true,
                    
                    AuthenticationMethod = validatingConnectionEventArgs.AuthenticationMethod,
                    AuthenticationData = validatingConnectionEventArgs.ResponseAuthenticationData,
                    AssignedClientIdentifier = validatingConnectionEventArgs.AssignedClientIdentifier,
                    ReasonString = validatingConnectionEventArgs.ReasonString,
                    ServerReference = validatingConnectionEventArgs.ServerReference
                }
            };

            if (validatingConnectionEventArgs.ResponseUserProperties != null)
            {
                connAckPacket.Properties.UserProperties.AddRange(validatingConnectionEventArgs.ResponseUserProperties);
            }
            
            return connAckPacket;
        }
    }
}