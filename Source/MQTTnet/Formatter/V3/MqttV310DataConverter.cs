using System;
using System.Linq;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter.V3
{
    public class MqttV310DataConverter : IMqttDataConverter
    {
        public MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            if (applicationMessage.UserProperties?.Any() == true)
            {
                throw new MqttProtocolViolationException("User properties are not supported in MQTT version 3.");
            }

            return new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false
            };
        }

        public MqttApplicationMessage CreateApplicationMessage(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttApplicationMessage
            {
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = publishPacket.QualityOfServiceLevel,
                Retain = publishPacket.Retain
            };
        }

        public MqttClientConnectResult CreateClientConnectResult(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            MqttClientConnectResultCode resultCode;
            switch (connAckPacket.ReturnCode.Value)
            {
                case MqttConnectReturnCode.ConnectionAccepted:
                    {
                        resultCode = MqttClientConnectResultCode.Success;
                        break;
                    }

                case MqttConnectReturnCode.ConnectionRefusedUnacceptableProtocolVersion:
                    {
                        resultCode = MqttClientConnectResultCode.UnsupportedProtocolVersion;
                        break;
                    }

                case MqttConnectReturnCode.ConnectionRefusedNotAuthorized:
                    {
                        resultCode = MqttClientConnectResultCode.NotAuthorized;
                        break;
                    }

                case MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword:
                    {
                        resultCode = MqttClientConnectResultCode.BadUserNameOrPassword;
                        break;
                    }

                case MqttConnectReturnCode.ConnectionRefusedIdentifierRejected:
                    {
                        resultCode = MqttClientConnectResultCode.ClientIdentifierNotValid;
                        break;
                    }

                case MqttConnectReturnCode.ConnectionRefusedServerUnavailable:
                    {
                        resultCode = MqttClientConnectResultCode.ServerUnavailable;
                        break;
                    }

                default:
                    throw new MqttProtocolViolationException("Received unexpected return code.");
            }

            return new MqttClientConnectResult
            {
                IsSessionPresent = connAckPacket.IsSessionPresent,
                ResultCode = resultCode
            };
        }

        public MqttConnectPacket CreateConnectPacket(MqttApplicationMessage willApplicationMessage, IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new MqttConnectPacket
            {
                ClientId = options.ClientId,
                Username = options.Credentials?.Username,
                Password = options.Credentials?.Password,
                CleanSession = options.CleanSession,
                KeepAlivePeriod = (ushort)options.KeepAlivePeriod.TotalSeconds,
                WillMessage = willApplicationMessage
            };
        }

        public MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subAckPacket == null) throw new ArgumentNullException(nameof(subAckPacket));

            if (subAckPacket.ReturnCodes.Count != subscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException("The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientSubscribeResult();

            result.Items.AddRange(subscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientSubscribeResultItem(t, (MqttClientSubscribeResultCode)subAckPacket.ReturnCodes[i])));

            return result;
        }

        public MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubAckPacket == null) throw new ArgumentNullException(nameof(unsubAckPacket));
            
            var result = new MqttClientUnsubscribeResult();

            result.Items.AddRange(unsubscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientUnsubscribeResultItem(t, MqttClientUnsubscribeResultCode.Success)));

            return result;
        }

        public MqttSubscribePacket CreateSubscribePacket(MqttClientSubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.UserProperties?.Any() == true)
            {
                throw new MqttProtocolViolationException("User properties are not supported in MQTT version 3.");
            }

            var subscribePacket = new MqttSubscribePacket();
            subscribePacket.TopicFilters.AddRange(options.TopicFilters);
            
            return subscribePacket;
        }

        public MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (options.UserProperties?.Any() == true)
            {
                throw new MqttProtocolViolationException("User properties are not supported in MQTT version 3.");
            }

            var unsubscribePacket = new MqttUnsubscribePacket();
            unsubscribePacket.TopicFilters.AddRange(options.TopicFilters);

            return unsubscribePacket;
        }
    }
}
