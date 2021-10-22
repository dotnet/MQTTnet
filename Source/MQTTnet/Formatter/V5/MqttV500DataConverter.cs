using MQTTnet.Client.Connecting;
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
        public MqttPubAckPacket CreatePubAckPacket(MqttPublishPacket publishPacket,
            MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubAckReasonCode) (int) reasonCode
            };
        }

        public MqttPubRecPacket CreatePubRecPacket(MqttPublishPacket publishPacket,
            MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            return new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode) (int) reasonCode
            };
        }

        public MqttPubCompPacket CreatePubCompPacket(MqttPubRelPacket pubRelPacket,
            MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRelPacket == null) throw new ArgumentNullException(nameof(pubRelPacket));

            return new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = (MqttPubCompReasonCode) (int) reasonCode
            };
        }

        public MqttPubRelPacket CreatePubRelPacket(MqttPubRecPacket pubRecPacket,
            MqttApplicationMessageReceivedReasonCode reasonCode)
        {
            if (pubRecPacket == null) throw new ArgumentNullException(nameof(pubRecPacket));

            return new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier,
                ReasonCode = (MqttPubRelReasonCode) (int) reasonCode
            };
        }

        public MqttClientConnectResult CreateClientConnectResult(MqttConnAckPacket connAckPacket)
        {
            if (connAckPacket == null) throw new ArgumentNullException(nameof(connAckPacket));

            return new MqttClientConnectResult
            {
                IsSessionPresent = connAckPacket.IsSessionPresent,
                ResultCode = (MqttClientConnectResultCode) (int) connAckPacket.ReasonCode,
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
                TopicAliasMaximum = connAckPacket.Properties?.TopicAliasMaximum ?? 0,
                ServerReference = connAckPacket.Properties?.ServerReference,
                ServerKeepAlive = connAckPacket.Properties?.ServerKeepAlive,
                SessionExpiryInterval = connAckPacket.Properties?.SessionExpiryInterval,
                SubscriptionIdentifiersAvailable = connAckPacket.Properties?.SubscriptionIdentifiersAvailable ?? true,
                SharedSubscriptionAvailable = connAckPacket.Properties?.SharedSubscriptionAvailable ?? true,
                UserProperties = connAckPacket.Properties?.UserProperties ?? new List<MqttUserProperty>()
            };
        }
        
        public MqttConnAckPacket CreateConnAckPacket(MqttConnectionValidatorContext connectionValidatorContext)
        {
            if (connectionValidatorContext == null) throw new ArgumentNullException(nameof(connectionValidatorContext));

            var connAckPacket = new MqttConnAckPacket
            {
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

        public MqttClientSubscribeResult CreateClientSubscribeResult(MqttSubscribePacket subscribePacket,
            MqttSubAckPacket subAckPacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subAckPacket == null) throw new ArgumentNullException(nameof(subAckPacket));

            if (subAckPacket.ReasonCodes.Count != subscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException(
                    "The reason codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientSubscribeResult();

            result.Items.AddRange(subscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientSubscribeResultItem(t, (MqttClientSubscribeResultCode) subAckPacket.ReasonCodes[i])));

            return result;
        }

        public MqttClientUnsubscribeResult CreateClientUnsubscribeResult(MqttUnsubscribePacket unsubscribePacket,
            MqttUnsubAckPacket unsubAckPacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubAckPacket == null) throw new ArgumentNullException(nameof(unsubAckPacket));

            if (unsubAckPacket.ReasonCodes.Count != unsubscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException(
                    "The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientUnsubscribeResult();

            result.Items.AddRange(unsubscribePacket.TopicFilters.Select((t, i) =>
                new MqttClientUnsubscribeResultItem(t,
                    (MqttClientUnsubscribeResultCode) unsubAckPacket.ReasonCodes[i])));

            return result;
        }
        
        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subscribeResult == null) throw new ArgumentNullException(nameof(subscribeResult));

            var subAckPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };

            subAckPacket.ReasonCodes.AddRange(subscribeResult.ReasonCodes);
            
            return subAckPacket;
        }
        
        public MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket,
            UnsubscribeResult unsubscribeResult)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubscribeResult == null) throw new ArgumentNullException(nameof(unsubscribeResult));

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            unsubAckPacket.ReasonCodes.AddRange(unsubscribeResult.ReasonCodes);
            
            return unsubAckPacket;
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
                result.ReasonCode = (MqttClientPublishReasonCode) (int) pubAckPacket.ReasonCode;

                result.PacketIdentifier = pubAckPacket.PacketIdentifier;
            }

            return result;
        }

        public MqttClientPublishResult CreateClientPublishResult(MqttPubRecPacket pubRecPacket,
            MqttPubCompPacket pubCompPacket)
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

            if (pubRecPacket.ReasonCode != MqttPubRecReasonCode.Success)
            {
                // Both enums share the same values.
                result.ReasonCode = (MqttClientPublishReasonCode) pubRecPacket.ReasonCode;
            }

            return result;
        }
    }
}