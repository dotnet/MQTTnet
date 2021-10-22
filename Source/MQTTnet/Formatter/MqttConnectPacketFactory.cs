using System;
using MQTTnet.Client.Options;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttConnectPacketFactory
    {
        public MqttConnectPacket Create(IMqttClientOptions clientOptions)
        {
            if (clientOptions == null) throw new ArgumentNullException(nameof(clientOptions));
            
            var connectPacket = new MqttConnectPacket
            {
                ClientId = clientOptions.ClientId,
                Username = clientOptions.Credentials?.Username,
                Password = clientOptions.Credentials?.Password,
                CleanSession = clientOptions.CleanSession,
                KeepAlivePeriod = (ushort) clientOptions.KeepAlivePeriod.TotalSeconds,
                WillMessage = clientOptions.WillMessage,
                Properties =
                {
                    AuthenticationMethod = clientOptions.AuthenticationMethod,
                    AuthenticationData = clientOptions.AuthenticationData,
                    WillDelayInterval = clientOptions.WillDelayInterval,
                    MaximumPacketSize = clientOptions.MaximumPacketSize,
                    ReceiveMaximum = clientOptions.ReceiveMaximum,
                    RequestProblemInformation = clientOptions.RequestProblemInformation,
                    RequestResponseInformation = clientOptions.RequestResponseInformation,
                    SessionExpiryInterval = clientOptions.SessionExpiryInterval,
                    TopicAliasMaximum = clientOptions.TopicAliasMaximum
                }
            };

            if (clientOptions.UserProperties != null)
            {
                connectPacket.Properties.UserProperties.AddRange(clientOptions.UserProperties);
            }

            return connectPacket;
        }
    }
}