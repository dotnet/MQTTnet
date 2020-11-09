using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using MqttClientSubscribeResult = MQTTnet.Client.Subscribing.MqttClientSubscribeResult;

namespace MQTTnet.Formatter.V3
{
    public class MqttV310DataConverter : IMqttDataConverter
    {
        public MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false
            };
        }

        public MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket)
        {
            return new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubAckReasonCode.Success
            };
        }

        public MqttBasePacket CreatePubRecPacket(MqttPublishPacket publishPacket)
        {
            return new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRecReasonCode.Success
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

        public MqttClientAuthenticateResult CreateClientConnectResult(MqttConnAckPacket connAckPacket)
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

            return new MqttClientAuthenticateResult
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

        public MqttConnAckPacket CreateConnAckPacket(MqttConnectionValidatorContext connectionValidatorContext)
        {
            if (connectionValidatorContext == null) throw new ArgumentNullException(nameof(connectionValidatorContext));

            return new MqttConnAckPacket
            {
                ReturnCode = new MqttConnectReasonCodeConverter().ToConnectReturnCode(connectionValidatorContext.ReasonCode)
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

            var subscribePacket = new MqttSubscribePacket();
            subscribePacket.TopicFilters.AddRange(options.TopicFilters);

            return subscribePacket;
        }

        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, Server.MqttClientSubscribeResult subscribeResult)
        {
            var subackPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };

            subackPacket.ReturnCodes.AddRange(subscribeResult.ReturnCodes);

            return subackPacket;
        }

        public MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var unsubscribePacket = new MqttUnsubscribePacket();
            unsubscribePacket.TopicFilters.AddRange(options.TopicFilters);

            return unsubscribePacket;
        }

        public MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket, List<MqttUnsubscribeReasonCode> reasonCodes)
        {
            return new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier,
                ReasonCodes = reasonCodes
            };
        }

        public MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options)
        {
            if (options.ReasonCode != MqttClientDisconnectReason.NormalDisconnection || options.ReasonString != null)
            {
                throw new MqttProtocolViolationException("Reason codes and reason string for disconnect are only supported for MQTTv5.");
            }

            return new MqttDisconnectPacket();
        }

        public MqttClientPublishResult CreatePublishResult(MqttPubAckPacket pubAckPacket)
        {
            return new MqttClientPublishResult
            {
                PacketIdentifier = pubAckPacket?.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success
            };
        }

        public MqttClientPublishResult CreatePublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
        {
            if (pubRecPacket == null || pubCompPacket == null)
            {
                return new MqttClientPublishResult
                {
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError
                };
            }

            return new MqttClientPublishResult
            {
                PacketIdentifier = pubCompPacket.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success
            };
        }
    }
}
