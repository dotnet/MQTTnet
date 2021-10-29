using System;
using MQTTnet.Client;
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

            if (clientOptions.WillMessage != null)
            {
                connectPacket.WillFlag = true;
                connectPacket.WillTopic = clientOptions.WillMessage.Topic;
                connectPacket.WillQoS = clientOptions.WillMessage.QualityOfServiceLevel;
                connectPacket.WillMessage = clientOptions.WillMessage.Payload;
                connectPacket.WillRetain = clientOptions.WillMessage.Retain;
                connectPacket.WillProperties.ContentType = clientOptions.WillMessage.ContentType;
                connectPacket.WillProperties.CorrelationData = clientOptions.WillMessage.CorrelationData;
                connectPacket.WillProperties.ResponseTopic = clientOptions.WillMessage.ResponseTopic;
                connectPacket.WillProperties.MessageExpiryInterval = clientOptions.WillMessage.MessageExpiryInterval;
                connectPacket.WillProperties.PayloadFormatIndicator = clientOptions.WillMessage.PayloadFormatIndicator;
                //connectPacket.WillProperties.WillDelayInterval = clientOptions.WillMessage.;

                if (clientOptions.WillMessage.UserProperties != null)
                {
                    connectPacket.WillProperties.UserProperties.AddRange(clientOptions.WillMessage.UserProperties);
                }
            }

            if (clientOptions.UserProperties != null)
            {
                connectPacket.Properties.UserProperties.AddRange(clientOptions.UserProperties);
            }

            return connectPacket;
        }
    }
}