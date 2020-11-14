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

namespace MQTTnet.Formatter.V5
{
    public class MqttV500DataConverter : IMqttDataConverter
    {
        public MqttPublishPacket CreatePublishPacket(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var packet = new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false,
                Properties = new MqttPublishPacketProperties
                {
                    ContentType = applicationMessage.ContentType,
                    CorrelationData = applicationMessage.CorrelationData,
                    MessageExpiryInterval = applicationMessage.MessageExpiryInterval,
                    PayloadFormatIndicator = applicationMessage.PayloadFormatIndicator,
                    ResponseTopic = applicationMessage.ResponseTopic,
                    SubscriptionIdentifiers = applicationMessage.SubscriptionIdentifiers,
                    TopicAlias = applicationMessage.TopicAlias
                }
            };

            if (applicationMessage.UserProperties != null)
            {
                packet.Properties.UserProperties = new List<MqttUserProperty>();
                packet.Properties.UserProperties.AddRange(applicationMessage.UserProperties);
            }

            return packet;
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
            return new MqttApplicationMessage
            {
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = publishPacket.QualityOfServiceLevel,
                Retain = publishPacket.Retain,
                ResponseTopic = publishPacket.Properties?.ResponseTopic,
                ContentType = publishPacket.Properties?.ContentType,
                CorrelationData = publishPacket.Properties?.CorrelationData,
                MessageExpiryInterval = publishPacket.Properties?.MessageExpiryInterval,
                SubscriptionIdentifiers = publishPacket.Properties?.SubscriptionIdentifiers,
                TopicAlias = publishPacket.Properties?.TopicAlias,
                PayloadFormatIndicator = publishPacket.Properties?.PayloadFormatIndicator,
                UserProperties = publishPacket.Properties?.UserProperties ?? new List<MqttUserProperty>()
            };
        }

        public MqttClientAuthenticateResult CreateClientConnectResult(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            return new MqttClientAuthenticateResult
            {
                IsSessionPresent = connAckPacket.IsSessionPresent,
                ResultCode = (MqttClientConnectResultCode)connAckPacket.ReasonCode.Value,
                WildcardSubscriptionAvailable = connAckPacket.Properties?.WildcardSubscriptionAvailable,
                RetainAvailable = connAckPacket.Properties?.RetainAvailable,
                AssignedClientIdentifier = connAckPacket.Properties?.AssignedClientIdentifier,
                AuthenticationMethod = connAckPacket.Properties?.AuthenticationMethod,
                AuthenticationData = connAckPacket.Properties?.AuthenticationData,
                MaximumPacketSize = connAckPacket.Properties?.MaximumPacketSize,
                ReasonString = connAckPacket.Properties?.ReasonString,
                ReceiveMaximum = connAckPacket.Properties?.ReceiveMaximum,
                MaximumQoS = connAckPacket.Properties?.MaximumQoS ?? MqttQualityOfServiceLevel.ExactlyOnce,
                ResponseInformation = connAckPacket.Properties?.ResponseInformation,
                TopicAliasMaximum = connAckPacket.Properties?.TopicAliasMaximum,
                ServerReference = connAckPacket.Properties?.ServerReference,
                ServerKeepAlive = connAckPacket.Properties?.ServerKeepAlive,
                SessionExpiryInterval = connAckPacket.Properties?.SessionExpiryInterval,
                SubscriptionIdentifiersAvailable = connAckPacket.Properties?.SubscriptionIdentifiersAvailable,
                SharedSubscriptionAvailable = connAckPacket.Properties?.SharedSubscriptionAvailable,
                UserProperties = connAckPacket.Properties?.UserProperties
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
                WillMessage = willApplicationMessage,
                Properties = new MqttConnectPacketProperties
                {
                    AuthenticationMethod = options.AuthenticationMethod,
                    AuthenticationData = options.AuthenticationData,
                    WillDelayInterval = options.WillDelayInterval,
                    MaximumPacketSize = options.MaximumPacketSize,
                    ReceiveMaximum = options.ReceiveMaximum,
                    RequestProblemInformation = options.RequestProblemInformation,
                    RequestResponseInformation = options.RequestResponseInformation,
                    SessionExpiryInterval = options.SessionExpiryInterval,
                    TopicAliasMaximum = options.TopicAliasMaximum,
                    UserProperties = options.UserProperties
                }
            };
        }

        public MqttConnAckPacket CreateConnAckPacket(MqttConnectionValidatorContext connectionValidatorContext)
        {
            return new MqttConnAckPacket
            {
                ReasonCode = connectionValidatorContext.ReasonCode,
                Properties = new MqttConnAckPacketProperties
                {
                    UserProperties = connectionValidatorContext.ResponseUserProperties,
                    AuthenticationMethod = connectionValidatorContext.AuthenticationMethod,
                    AuthenticationData = connectionValidatorContext.ResponseAuthenticationData,
                    AssignedClientIdentifier = connectionValidatorContext.AssignedClientIdentifier,
                    ReasonString = connectionValidatorContext.ReasonString,
                    TopicAliasMaximum = ushort.MaxValue
                }
            };
        }

        public MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subAckPacket == null) throw new ArgumentNullException(nameof(subAckPacket));

            if (subAckPacket.ReasonCodes.Count != subscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException("The reason codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientSubscribeResult();

            result.Items.AddRange(subscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientSubscribeResultItem(t, (MqttClientSubscribeResultCode)subAckPacket.ReasonCodes[i])));

            return result;
        }

        public MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubAckPacket == null) throw new ArgumentNullException(nameof(unsubAckPacket));

            if (unsubAckPacket.ReasonCodes.Count != unsubscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException("The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientUnsubscribeResult();

            result.Items.AddRange(unsubscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientUnsubscribeResultItem(t, (MqttClientUnsubscribeResultCode)unsubAckPacket.ReasonCodes[i])));

            return result;
        }

        public MqttSubscribePacket CreateSubscribePacket(MqttClientSubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var packet = new MqttSubscribePacket
            {
                Properties = new MqttSubscribePacketProperties()
            };

            packet.TopicFilters.AddRange(options.TopicFilters);
            packet.Properties.SubscriptionIdentifier = options.SubscriptionIdentifier;
            packet.Properties.UserProperties = options.UserProperties;

            return packet;
        }

        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, Server.MqttClientSubscribeResult subscribeResult)
        {
            var subackPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };

            subackPacket.ReasonCodes.AddRange(subscribeResult.ReasonCodes);

            return subackPacket;
        }

        public MqttUnsubscribePacket CreateUnsubscribePacket(MqttClientUnsubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var packet = new MqttUnsubscribePacket
            {
                Properties = new MqttUnsubscribePacketProperties()
            };

            packet.TopicFilters.AddRange(options.TopicFilters);
            packet.Properties.UserProperties = options.UserProperties;

            return packet;
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
            var packet = new MqttDisconnectPacket();

            if (options == null)
            {
                packet.ReasonCode = MqttDisconnectReasonCode.NormalDisconnection;
            }
            else
            {
                packet.ReasonCode = (MqttDisconnectReasonCode)options.ReasonCode;
            }

            return packet;
        }

        public MqttClientPublishResult CreatePublishResult(MqttPubAckPacket pubAckPacket)
        {
            var result = new MqttClientPublishResult
            {
                ReasonCode = MqttClientPublishReasonCode.Success,
                ReasonString = pubAckPacket?.Properties?.ReasonString,
                UserProperties = pubAckPacket?.Properties?.UserProperties
            };

            if (pubAckPacket != null)
            {
                // QoS 0 has no response. So we treat it as a success always.
                // Both enums have the same values. So it can be easily converted.
                result.ReasonCode = (MqttClientPublishReasonCode)pubAckPacket.ReasonCode;

                result.PacketIdentifier = pubAckPacket.PacketIdentifier;
            }

            return result;
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

            // The PUBCOMP is the last packet in QoS 2. So we use the results from that instead of PUBREC.
            if (pubCompPacket.ReasonCode == MqttPubCompReasonCode.PacketIdentifierNotFound)
            {
                return new MqttClientPublishResult
                {
                    PacketIdentifier = pubCompPacket.PacketIdentifier,
                    ReasonCode = MqttClientPublishReasonCode.UnspecifiedError,
                    ReasonString = pubCompPacket.Properties?.ReasonString,
                    UserProperties = pubCompPacket.Properties?.UserProperties
                };
            }

            var result = new MqttClientPublishResult
            {
                PacketIdentifier = pubCompPacket.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success,
                ReasonString = pubCompPacket.Properties?.ReasonString,
                UserProperties = pubCompPacket.Properties?.UserProperties
            };

            if (pubRecPacket.ReasonCode.HasValue)
            {
                // Both enums share the same values.
                result.ReasonCode = (MqttClientPublishReasonCode)pubRecPacket.ReasonCode.Value;
            }

            return result;
        }
    }
}
