using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        // We use a reader writer lock because the subscriptions are only read most of the time.
        // Since writing is done by multiple threads (server API or connection thread), we cannot avoid locking
        // completely by swapping references etc.
        readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();
        readonly Dictionary<string, MqttTopicFilter> _subscriptions = new Dictionary<string, MqttTopicFilter>();
        readonly MqttClientSession _clientSession;
        readonly IMqttServerOptions _options;
        readonly MqttServerEventDispatcher _eventDispatcher;

        public MqttClientSubscriptionsManager(MqttClientSession clientSession, MqttServerEventDispatcher eventDispatcher, IMqttServerOptions serverOptions)
        {
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _options = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
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

                if (!interceptorContext.AcceptSubscription || string.IsNullOrEmpty(finalTopicFilter?.Topic))
                {
                    continue;
                }

                _subscriptionsLock.EnterWriteLock();
                try
                {
                    _subscriptions[finalTopicFilter.Topic] = finalTopicFilter;
                }
                finally
                {
                    _subscriptionsLock.ExitWriteLock();
                }

                await _eventDispatcher.SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, finalTopicFilter).ConfigureAwait(false);
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

                if (!interceptorContext.AcceptSubscription)
                {
                    continue;
                }

                _subscriptionsLock.EnterWriteLock();
                try
                {
                    _subscriptions[topicFilter.Topic] = topicFilter;
                }
                finally
                {
                    _subscriptionsLock.ExitWriteLock();
                }

                await _eventDispatcher.SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, topicFilter).ConfigureAwait(false);
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

                _subscriptionsLock.EnterWriteLock();
                try
                {
                    reasonCodes.Add(_subscriptions.Remove(topicFilter)
                        ? MqttUnsubscribeReasonCode.Success
                        : MqttUnsubscribeReasonCode.NoSubscriptionExisted);
                }
                finally
                {
                    _subscriptionsLock.ExitWriteLock();
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

                _subscriptionsLock.EnterWriteLock();
                try
                {
                    _subscriptions.Remove(topicFilter);
                }
                finally
                {
                    _subscriptionsLock.ExitWriteLock();
                }
            }
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel)
        {
            Dictionary<string, MqttTopicFilter> subscriptionsCopy;
            _subscriptionsLock.EnterReadLock();
            try
            {
                subscriptionsCopy = new Dictionary<string, MqttTopicFilter>(_subscriptions);
            }
            finally
            {
                _subscriptionsLock.ExitReadLock();
            }

            var qosLevels = new HashSet<MqttQualityOfServiceLevel>();
            foreach (var subscription in subscriptionsCopy)
            {
                if (!MqttTopicFilterComparer.IsMatch(topic, subscription.Key))
                {
                    continue;
                }
                
                qosLevels.Add(subscription.Value.QualityOfServiceLevel);
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
            return (MqttSubscribeReturnCode)(int)qualityOfServiceLevel;
        }

        static MqttSubscribeReasonCode ConvertToSubscribeReasonCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return (MqttSubscribeReasonCode)(int)qualityOfServiceLevel;
        }

        async Task<MqttSubscriptionInterceptorContext> InterceptSubscribeAsync(MqttTopicFilter topicFilter)
        {
            var context = new MqttSubscriptionInterceptorContext(_clientSession.ClientId, topicFilter, _clientSession.Items);
            if (_options.SubscriptionInterceptor != null)
            {
                await _options.SubscriptionInterceptor.InterceptSubscriptionAsync(context).ConfigureAwait(false);
            }

            return context;
        }

        async Task<MqttUnsubscriptionInterceptorContext> InterceptUnsubscribeAsync(string topicFilter)
        {
            var context = new MqttUnsubscriptionInterceptorContext(_clientSession.ClientId, topicFilter, _clientSession.Items);
            if (_options.UnsubscriptionInterceptor != null)
            {
                await _options.UnsubscriptionInterceptor.InterceptUnsubscriptionAsync(context).ConfigureAwait(false);
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
