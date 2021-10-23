using MQTTnet.Client.Connecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Linq;
using MQTTnet.Server.Internal;

namespace MQTTnet.Formatter.V3
{
    public class MqttV310DataConverter : IMqttDataConverter
    {
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
        
        public MqttSubAckPacket CreateSubAckPacket(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subscribeResult == null) throw new ArgumentNullException(nameof(subscribeResult));

            var subAckPacket = new MqttSubAckPacket
            {
                PacketIdentifier = subscribePacket.PacketIdentifier
            };
            
            subAckPacket.ReturnCodes.AddRange(subscribeResult.ReturnCodes);
            
            return subAckPacket;
        }
        
        public MqttUnsubAckPacket CreateUnsubAckPacket(MqttUnsubscribePacket unsubscribePacket, UnsubscribeResult _)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            
            // The UnsubscribeResult is not supported in protocol version 3.1.1!
            return new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };
        }
        
        public MqttClientPublishResult CreateClientPublishResult(MqttPubAckPacket pubAckPacket)
        {
            return new MqttClientPublishResult
            {
                PacketIdentifier = pubAckPacket?.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success
            };
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

            return new MqttClientPublishResult
            {
                PacketIdentifier = pubCompPacket.PacketIdentifier,
                ReasonCode = MqttClientPublishReasonCode.Success
            };
        }
    }
}
