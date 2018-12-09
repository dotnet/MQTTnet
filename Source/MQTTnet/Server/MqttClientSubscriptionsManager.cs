using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSubscriptionsManager
    {
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly IMqttServerOptions _options;
        private readonly MqttServerEventDispatcher _eventDispatcher;
        private readonly string _clientId;

        public MqttClientSubscriptionsManager(string clientId, IMqttServerOptions options, MqttServerEventDispatcher eventDispatcher)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        public MqttClientSubscribeResult Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var result = new MqttClientSubscribeResult
            {
                ResponsePacket = new MqttSubAckPacket
                {
                    PacketIdentifier = subscribePacket.PacketIdentifier
                },

                CloseConnection = false
            };

            foreach (var topicFilter in subscribePacket.TopicFilters)
            {
                var interceptorContext = InterceptSubscribe(topicFilter);
                if (!interceptorContext.AcceptSubscription)
                {
                    result.ResponsePacket.SubscribeReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                }
                else
                {
                    result.ResponsePacket.SubscribeReturnCodes.Add(ConvertToMaximumQoS(topicFilter.QualityOfServiceLevel));
                }

                if (interceptorContext.CloseConnection)
                {
                    result.CloseConnection = true;
                }

                if (interceptorContext.AcceptSubscription)
                {
                    lock (_subscriptions)
                    {
                        _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    }

                    _eventDispatcher.OnClientSubscribedTopic(_clientId, topicFilter);
                }
            }

            return result;
        }

        public MqttUnsubAckPacket Unsubscribe(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            lock (_subscriptions)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscriptions.Remove(topicFilter);

                    _eventDispatcher.OnClientUnsubscribedTopic(_clientId, topicFilter);
                }
            }

            return new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel)
        {
            var qosLevels = new HashSet<MqttQualityOfServiceLevel>();

            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    if (!MqttTopicFilterComparer.IsMatch(topic, subscription.Key))
                    {
                        continue;
                    }

                    qosLevels.Add(subscription.Value);
                }
            }

            if (qosLevels.Count == 0)
            {
                return new CheckSubscriptionsResult
                {
                    IsSubscribed = false
                };
            }

            return CreateSubscriptionResult(qosLevel, qosLevels);
        }

        private static MqttSubscribeReturnCode ConvertToMaximumQoS(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS2;
                default: return MqttSubscribeReturnCode.Failure;
            }
        }

        private MqttSubscriptionInterceptorContext InterceptSubscribe(TopicFilter topicFilter)
        {
            var interceptorContext = new MqttSubscriptionInterceptorContext(_clientId, topicFilter);
            _options.SubscriptionInterceptor?.Invoke(interceptorContext);
            return interceptorContext;
        }

        private static CheckSubscriptionsResult CreateSubscriptionResult(MqttQualityOfServiceLevel qosLevel, HashSet<MqttQualityOfServiceLevel> subscribedQoSLevels)
        {
            MqttQualityOfServiceLevel effectiveQoS;
            if (subscribedQoSLevels.Contains(qosLevel))
            {
                effectiveQoS = qosLevel;
            }
            else if (subscribedQoSLevels.Count == 1)
            {
                effectiveQoS = subscribedQoSLevels.First();
            }
            else
            {
                effectiveQoS = subscribedQoSLevels.Max();
            }

            return new CheckSubscriptionsResult
            {
                IsSubscribed = true,
                QualityOfServiceLevel = effectiveQoS
            };
        }
    }
}
