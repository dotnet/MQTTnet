using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        readonly Dictionary<string, MqttTopicFilter> _subscriptions = new Dictionary<string, MqttTopicFilter>();
        readonly MqttClientSession _clientSession;
        readonly IMqttServerOptions _serverOptions;
        readonly MqttServerEventDispatcher _eventDispatcher;

        public MqttClientSubscriptionsManager(MqttClientSession clientSession, MqttServerEventDispatcher eventDispatcher, IMqttServerOptions serverOptions)
        {
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));

            // TODO: Consider removing the server options here and build a new class "ISubscriptionInterceptor" and just pass it. The instance is generated in the root server class upon start.
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttSubscribePacket subscribePacket, MqttConnectPacket connectPacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));

            var result = new MqttClientSubscribeResult();

            foreach (var originalTopicFilter in subscribePacket.TopicFilters)
            {
                var interceptorContext = await InterceptSubscribeAsync(originalTopicFilter).ConfigureAwait(false);

                var finalTopicFilter = interceptorContext.TopicFilter;

                if (finalTopicFilter == null || string.IsNullOrEmpty(finalTopicFilter.Topic) || !interceptorContext.AcceptSubscription)
                {
                    result.ReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    result.ReasonCodes.Add(MqttSubscribeReasonCode.UnspecifiedError);
                }
                else
                {
                    result.ReturnCodes.Add(ConvertToSubscribeReturnCode(finalTopicFilter.QualityOfServiceLevel));
                    result.ReasonCodes.Add(ConvertToSubscribeReasonCode(finalTopicFilter.QualityOfServiceLevel));
                }

                if (interceptorContext.CloseConnection)
                {
                    result.CloseConnection = true;
                }

                if (interceptorContext.AcceptSubscription && !string.IsNullOrEmpty(finalTopicFilter?.Topic))
                {
                    lock (_subscriptions)
                    {
                        _subscriptions[finalTopicFilter.Topic] = finalTopicFilter;
                    }

                    await _eventDispatcher.SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, finalTopicFilter).ConfigureAwait(false);
                }
            }

            return result;
        }

        public async Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

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
                        _subscriptions[topicFilter.Topic] = topicFilter;
                    }

                    await _eventDispatcher.SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, topicFilter).ConfigureAwait(false);
                }
            }
        }

        public async Task<List<MqttUnsubscribeReasonCode>> UnsubscribeAsync(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var reasonCodes = new List<MqttUnsubscribeReasonCode>();

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                var interceptorContext = await InterceptUnsubscribeAsync(topicFilter).ConfigureAwait(false);
                if (!interceptorContext.AcceptUnsubscription)
                {
                    reasonCodes.Add(MqttUnsubscribeReasonCode.ImplementationSpecificError);
                    continue;
                }

                lock (_subscriptions)
                {
                    if (_subscriptions.Remove(topicFilter))
                    {
                        reasonCodes.Add(MqttUnsubscribeReasonCode.Success);
                    }
                    else
                    {
                        reasonCodes.Add(MqttUnsubscribeReasonCode.NoSubscriptionExisted);
                    }
                }
            }

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                await _eventDispatcher.SafeNotifyClientUnsubscribedTopicAsync(_clientSession.ClientId, topicFilter).ConfigureAwait(false);
            }

            return reasonCodes;
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            foreach (var topicFilter in topicFilters)
            {
                var interceptorContext = await InterceptUnsubscribeAsync(topicFilter).ConfigureAwait(false);
                if (!interceptorContext.AcceptUnsubscription)
                {
                    continue;
                }

                lock (_subscriptions)
                {
                    _subscriptions.Remove(topicFilter);
                }
            }
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

                    qosLevels.Add(subscription.Value.QualityOfServiceLevel);
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

        static MqttSubscribeReturnCode ConvertToSubscribeReturnCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReturnCode.SuccessMaximumQoS2;
                default: return MqttSubscribeReturnCode.Failure;
            }
        }

        static MqttSubscribeReasonCode ConvertToSubscribeReasonCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce: return MqttSubscribeReasonCode.GrantedQoS0;
                case MqttQualityOfServiceLevel.AtLeastOnce: return MqttSubscribeReasonCode.GrantedQoS1;
                case MqttQualityOfServiceLevel.ExactlyOnce: return MqttSubscribeReasonCode.GrantedQoS2;
                default: return MqttSubscribeReasonCode.UnspecifiedError;
            }
        }

        async Task<MqttSubscriptionInterceptorContext> InterceptSubscribeAsync(MqttTopicFilter topicFilter)
        {
            var context = new MqttSubscriptionInterceptorContext(_clientSession.ClientId, topicFilter, _clientSession.Items);
            if (_serverOptions.SubscriptionInterceptor != null)
            {
                await _serverOptions.SubscriptionInterceptor.InterceptSubscriptionAsync(context).ConfigureAwait(false);
            }

            return context;
        }

        async Task<MqttUnsubscriptionInterceptorContext> InterceptUnsubscribeAsync(string topicFilter)
        {
            var context = new MqttUnsubscriptionInterceptorContext(_clientSession.ClientId, topicFilter, _clientSession.Items);
            if (_serverOptions.UnsubscriptionInterceptor != null)
            {
                await _serverOptions.UnsubscriptionInterceptor.InterceptUnsubscriptionAsync(context).ConfigureAwait(false);
            }

            return context;
        }

        static CheckSubscriptionsResult CreateSubscriptionResult(MqttQualityOfServiceLevel qosLevel, HashSet<MqttQualityOfServiceLevel> subscribedQoSLevels)
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
