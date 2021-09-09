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

namespace MQTTnet.Formatter.V5
{
    public sealed class MqttV500DataConverter : IMqttDataConverter
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
                Dup = applicationMessage.Dup,
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

        public MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubAckReasonCode)(int)reasonCode
            };
        }

        public MqttPubRecPacket CreatePubRecPacket(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode)(int)reasonCode
            };
        }

        public MqttPubCompPacket CreatePubCompPacket(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRelPacket == null) throw new ArgumentNullException(nameof(pubRelPacket));

            return new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = (MqttPubCompReasonCode)(int)reasonCode
            };
        }

        public MqttPubRelPacket CreatePubRelPacket(MqttPubRecPacket pubRecPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRecPacket == null) throw new ArgumentNullException(nameof(pubRecPacket));

            return new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier,
                ReasonCode = (MqttPubRelReasonCode)(int)reasonCode
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
                Dup = publishPacket.Dup,
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
                ResultCode = (MqttClientConnectResultCode)(int)connAckPacket.ReasonCode,
                WildcardSubscriptionAvailable = connAckPacket.Properties?.WildcardSubscriptionAvailable ?? true,
                RetainAvailable = connAckPacket.Properties?.RetainAvailable ?? true,
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
                SubscriptionIdentifiersAvailable = connAckPacket.Properties?.SubscriptionIdentifiersAvailable ?? true,
                SharedSubscriptionAvailable = connAckPacket.Properties?.SharedSubscriptionAvailable ?? true,
                UserProperties = connAckPacket.Properties?.UserProperties ?? new List<MqttUserProperty>()
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
            if (connectionValidatorContext == null) throw new ArgumentNullException(nameof(connectionValidatorContext));

            return new MqttConnAckPacket
            {
                ReasonCode = connectionValidatorContext.ReasonCode,
                Properties = new MqttConnAckPacketProperties
                {
                    RetainAvailable = true,
                    SubscriptionIdentifiersAvailable = true,
                    SharedSubscriptionAvailable = false,
                    TopicAliasMaximum = ushort.MaxValue,
                    WildcardSubscriptionAvailable = true,
                    
                    UserProperties = connectionValidatorContext.ResponseUserProperties,
                    AuthenticationMethod = connectionValidatorContext.AuthenticationMethod,
                    AuthenticationData = connectionValidatorContext.ResponseAuthenticationData,
                    AssignedClientIdentifier = connectionValidatorContext.AssignedClientIdentifier,
                    ReasonString = connectionValidatorContext.ReasonString
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

        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subscribeResult == null) throw new ArgumentNullException(nameof(subscribeResult));

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

        public MqttClientPublishResult CreateClientPublishResult(MqttPubAckPacket pubAckPacket)
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
                result.ReasonCode = (MqttClientPublishReasonCode)(int)(pubAckPacket.ReasonCode ?? 0);

                result.PacketIdentifier = pubAckPacket.PacketIdentifier;
            }

            return result;
        }

        public MqttClientPublishResult CreateClientPublishResult(MqttPubRecPacket pubRecPacket, MqttPubCompPacket pubCompPacket)
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
                result.ReasonCode = (MqttClientPublishReasonCode)(pubRecPacket.ReasonCode ?? 0);
            }

            return result;
        }
    }
}