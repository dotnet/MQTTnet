using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public MqttClientSubscriptionsManager(string clientId, MqttServerEventDispatcher eventDispatcher, IMqttServerOptions options)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttSubscribePacket subscribePacket)
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
                var interceptorContext = await InterceptSubscribeAsync(topicFilter).ConfigureAwait(false);
                if (!interceptorContext.AcceptSubscription)
                {
                    result.ResponsePacket.ReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    result.ResponsePacket.ReasonCodes.Add(MqttSubscribeReasonCode.UnspecifiedError);
                }
                else
                {
                    result.ResponsePacket.ReturnCodes.Add(ConvertToSubscribeReturnCode(topicFilter.QualityOfServiceLevel));
                    result.ResponsePacket.ReasonCodes.Add(ConvertToSubscribeReasonCode(topicFilter.QualityOfServiceLevel));
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

                    await _eventDispatcher.HandleClientSubscribedTopicAsync(_clientId, topicFilter).ConfigureAwait(false);
                }
            }

            return result;
        }

        public async Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            foreach (var topicFilter in topicFilters)
            {
                var interceptorContext = await InterceptSubscribeAsync(topicFilter).ConfigureAwait(false);
                if (!interceptorContext.AcceptSubscription)
                {
                   continue;
                }

                if (interceptorContext.AcceptSubscription)
                {
                    lock (_subscriptions)
                    {
                        _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    }

                    await _eventDispatcher.HandleClientSubscribedTopicAsync(_clientId, topicFilter).ConfigureAwait(false);
                }
            }
        }

        public async Task<MqttUnsubAckPacket> UnsubscribeAsync(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            lock (_subscriptions)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    if (_subscriptions.Remove(topicFilter))
                    {
                        unsubAckPacket.ReasonCodes.Add(MqttUnsubscribeReasonCode.Success);
                    }
                    else
                    {
                        unsubAckPacket.ReasonCodes.Add(MqttUnsubscribeReasonCode.NoSubscriptionExisted);
                    }
                }
            }

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                await _eventDispatcher.HandleClientUnsubscribedTopicAsync(_clientId, topicFilter).ConfigureAwait(false);
            }
            
            return unsubAckPacket;
        }

        public Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            lock (_subscriptions)
            {
                foreach (var topicFilter in topicFilters)
                {
                    _subscriptions.Remove(topicFilter);
                }
            }

            return Task.FromResult(0);
        }

        public MqttQualityOfServiceLevel? CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel)
        {
            MqttQualityOfServiceLevel? subscribedQosLevel = null;

            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    if (!MqttTopicFilterComparer.IsMatch(topic, subscription.Key))
                    {
                        continue;
                    }

                    if (subscribedQosLevel.HasValue)
                    {
                        subscribedQosLevel = qosLevel |= subscription.Value;
                    }
                    else
                    {
                        subscribedQosLevel = subscription.Value;
                    }
                }
            }

            if (!subscribedQosLevel.HasValue)
            {
                return null;
            }

            return (MqttQualityOfServiceLevel)Math.Min((byte)subscribedQosLevel.Value, (byte)qosLevel);
        }

        private static MqttSubscribeReturnCode ConvertToSubscribeReturnCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS2;
                default: return MqttSubscribeReturnCode.Failure;
            }
        }

        private static MqttSubscribeReasonCode ConvertToSubscribeReasonCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReasonCode.GrantedQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReasonCode.GrantedQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReasonCode.GrantedQoS2;
                default: return MqttSubscribeReasonCode.UnspecifiedError;
            }
        }

        private async Task<MqttSubscriptionInterceptorContext> InterceptSubscribeAsync(TopicFilter topicFilter)
        {
            var context = new MqttSubscriptionInterceptorContext(_clientId, topicFilter);
            if (_options.SubscriptionInterceptor != null)
            {
                await _options.SubscriptionInterceptor.InterceptSubscriptionAsync(context).ConfigureAwait(false);
            }

            return context;
        }
    }
}
