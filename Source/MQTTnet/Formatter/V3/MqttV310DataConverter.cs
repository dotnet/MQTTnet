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
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter.V3
{
    public class MqttV310DataConverter : IMqttDataConverter
    {
        public MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var packet = MqttPublishPacket.GetInstance();

            packet.Topic = applicationMessage.Topic;
            packet.Payload = applicationMessage.Payload;
            packet.QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel;
            packet.Retain = applicationMessage.Retain;
            packet.Dup = false;

            return packet;
        }

        public MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));
            
            return new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier
            };
        }

        public MqttPubRecPacket CreatePubRecPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));
            
            return new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier
            };
        }

        public MqttPubCompPacket CreatePubCompPacket(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRelPacket == null) throw new ArgumentNullException(nameof(pubRelPacket));
            
            return new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier
            };
        }

        public MqttPubRelPacket CreatePubRelPacket(MqttPubRecPacket pubRecPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRecPacket == null) throw new ArgumentNullException(nameof(pubRecPacket));
            
            return new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier
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
                Retain = publishPacket.Retain,
                Dup = publishPacket.Dup
            };
        }

        public MqttClientConnectResult CreateClientConnectResult(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            MqttClientConnectResultCode resultCode;
            switch (connAckPacket.ReturnCode)
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
                RetainAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
                WildcardSubscriptionAvailable = true, // Always true because v3.1.1 does not have a way to "disable" that feature.
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

        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subscribeResult == null) throw new ArgumentNullException(nameof(subscribeResult));

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
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (reasonCodes == null) throw new ArgumentNullException(nameof(reasonCodes));

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier,
                ReasonCodes = reasonCodes
            };
        }

        public MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options)
        {
            return new MqttDisconnectPacket();
        }

        public MqttClientPublishResult CreateClientPublishResult(MqttPubAckPacket pubAckPacket)
        {
            var mqttClientPublishResult = MqttClientPublishResult.GetInstance();
            mqttClientPublishResult.PacketIdentifier = pubAckPacket?.PacketIdentifier;
            mqttClientPublishResult.ReasonCode = MqttClientPublishReasonCode.Success;

            return mqttClientPublishResult;
        }

        public MqttClientPublishResult CreateClientPublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
        {
            var mqttClientPublishResult = MqttClientPublishResult.GetInstance();

            if (pubRecPacket == null || pubCompPacket == null)
            {
                mqttClientPublishResult.ReasonCode = MqttClientPublishReasonCode.UnspecifiedError;
            }
            else
            {
                mqttClientPublishResult.PacketIdentifier = pubCompPacket.PacketIdentifier;
                mqttClientPublishResult.ReasonCode = MqttClientPublishReasonCode.Success;
            }

            return mqttClientPublishResult;
        }
    }
}
