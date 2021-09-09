using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSubscriptionsManager
    {
        // We use a reader writer lock because the subscriptions are only read most of the time.
        // Since writing is done by multiple threads (server API or connection thread), we cannot avoid locking
        // completely by swapping references etc.
        readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();
        readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>(4096);
        readonly MqttClientSession _clientSession;
        readonly IMqttServerOptions _options;
        readonly MqttServerEventDispatcher _eventDispatcher;
        readonly IMqttRetainedMessagesManager _retainedMessagesManager;

        public MqttClientSubscriptionsManager(
            MqttClientSession clientSession,
            IMqttServerOptions serverOptions,
            MqttServerEventDispatcher eventDispatcher,
            IMqttRetainedMessagesManager retainedMessagesManager)
        {
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _options = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public async Task<SubscribeResult> Subscribe(MqttSubscribePacket subscribePacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var retainedApplicationMessages = await _retainedMessagesManager.GetMessagesAsync().ConfigureAwait(false);
            var result = new SubscribeResult();

            // The topic filters are order by its QoS so that the higher QoS will win over a
            // lower one.
            foreach (var originalTopicFilter in subscribePacket.TopicFilters.OrderByDescending(f => f.QualityOfServiceLevel))
            {
                var interceptorContext = await InterceptSubscribe(originalTopicFilter).ConfigureAwait(false);
                var finalTopicFilter = interceptorContext?.TopicFilter ?? originalTopicFilter;
                var acceptSubscription = interceptorContext?.AcceptSubscription ?? true;
                var closeConnection = interceptorContext?.CloseConnection ?? false;

                if (string.IsNullOrEmpty(finalTopicFilter.Topic) || !acceptSubscription)
                {
                    result.ReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    result.ReasonCodes.Add(MqttSubscribeReasonCode.UnspecifiedError);
                }
                else
                {
                    result.ReturnCodes.Add(ConvertToSubscribeReturnCode(finalTopicFilter.QualityOfServiceLevel));
                    result.ReasonCodes.Add(ConvertToSubscribeReasonCode(finalTopicFilter.QualityOfServiceLevel));
                }

                if (closeConnection)
                {
                    result.CloseConnection = true;
                }

                if (!acceptSubscription || string.IsNullOrEmpty(finalTopicFilter.Topic))
                {
                    continue;
                }

                var subscription = CreateSubscription(finalTopicFilter, subscribePacket);

                await _eventDispatcher.SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, finalTopicFilter).ConfigureAwait(false);

                FilterRetainedApplicationMessages(retainedApplicationMessages, subscription, result);
            }

            return result;
        }

        public async Task<List<MqttUnsubscribeReasonCode>> Unsubscribe(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var reasonCodes = new List<MqttUnsubscribeReasonCode>();

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                var interceptorContext = await InterceptUnsubscribe(topicFilter).ConfigureAwait(false);
                if (interceptorContext != null && !interceptorContext.AcceptUnsubscription)
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

        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel, string senderClientId)
        {
            List<Subscription> subscriptions;
            _subscriptionsLock.EnterReadLock();
            try
            {
                subscriptions = _subscriptions.Values.ToList();
            }
            finally
            {
                _subscriptionsLock.ExitReadLock();
            }

            var senderIsReceiver = string.Equals(senderClientId, _clientSession.ClientId);

            var qosLevels = new HashSet<MqttQualityOfServiceLevel>();
            var subscriptionIdentifiers = new HashSet<uint>();
            var retainAsPublished = false;

            foreach (var subscription in subscriptions)
            {
                if (subscription.NoLocal && senderIsReceiver)
                {
                    // This is a MQTTv5 feature!
                    continue;
                }

                if (subscription.RetainAsPublished)
                {
                    // This is a MQTTv5 feature!
                    retainAsPublished = true;
                }

                if (!MqttTopicFilterComparer.IsMatch(topic, subscription.Topic))
                {
                    continue;
                }

                qosLevels.Add(subscription.QualityOfServiceLevel);

                if (subscription.Identifier > 0)
                {
                    subscriptionIdentifiers.Add(subscription.Identifier);
                }
            }

            if (qosLevels.Count == 0)
            {
                return CheckSubscriptionsResult.NotSubscribed;
            }

            return new CheckSubscriptionsResult
            {
                IsSubscribed = true,
                RetainAsPublished = retainAsPublished,
                SubscriptionIdentifiers = subscriptionIdentifiers.ToList(),
                QualityOfServiceLevel = GetEffectiveQoS(qosLevel, qosLevels)
            };
        }

        Subscription CreateSubscription(MqttTopicFilter topicFilter, MqttSubscribePacket subscribePacket)
        {
            var subscription = new Subscription
            {
                Topic = topicFilter.Topic,
                NoLocal = topicFilter.NoLocal,
                RetainHandling = topicFilter.RetainHandling,
                RetainAsPublished = topicFilter.RetainAsPublished,
                QualityOfServiceLevel = topicFilter.QualityOfServiceLevel,
                Identifier = subscribePacket.Properties?.SubscriptionIdentifier ?? 0
            };

            _subscriptionsLock.EnterWriteLock();
            try
            {
                subscription.IsNewSubscription = !_subscriptions.ContainsKey(topicFilter.Topic);
                _subscriptions[topicFilter.Topic] = subscription;
            }
            finally
            {
                _subscriptionsLock.ExitWriteLock();
            }

            return subscription;
        }

        static void FilterRetainedApplicationMessages(IList<MqttApplicationMessage> retainedApplicationMessages, Subscription subscription, SubscribeResult subscribeResult)
        {
            for (var i = retainedApplicationMessages.Count - 1; i >= 0; i--)
            {
                var retainedApplicationMessage = retainedApplicationMessages[i];
                if (retainedApplicationMessage == null)
                {
                    continue;
                }

                if (subscription.RetainHandling == MqttRetainHandling.DoNotSendOnSubscribe)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (subscription.RetainHandling == MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly && !subscription.IsNewSubscription)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (!MqttTopicFilterComparer.IsMatch(retainedApplicationMessage.Topic, subscription.Topic))
                {
                    continue;
                }

                var queuedApplicationMessage = new MqttQueuedApplicationMessage
                {
                    ApplicationMessage = retainedApplicationMessage,
                    IsRetainedMessage = true,
                    SubscriptionQualityOfServiceLevel = subscription.QualityOfServiceLevel
                };

                if (subscription.Identifier > 0)
                {
                    queuedApplicationMessage.SubscriptionIdentifiers = new List<uint> { subscription.Identifier };
                }

                subscribeResult.RetainedApplicationMessages.Add(queuedApplicationMessage);

                retainedApplicationMessages[i] = null;
            }
        }

        static MqttSubscribeReturnCode ConvertToSubscribeReturnCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return (MqttSubscribeReturnCode)(int)qualityOfServiceLevel;
        }

        static MqttSubscribeReasonCode ConvertToSubscribeReasonCode(MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return (MqttSubscribeReasonCode)(int)qualityOfServiceLevel;
        }

        async Task<MqttSubscriptionInterceptorContext> InterceptSubscribe(MqttTopicFilter topicFilter)
        {
            var interceptor = _options.SubscriptionInterceptor;
            if (interceptor == null)
            {
                return null;
            }

            var context = new MqttSubscriptionInterceptorContext
            {
                ClientId = _clientSession.ClientId,
                TopicFilter = topicFilter,
                SessionItems = _clientSession.Items
            };

            await interceptor.InterceptSubscriptionAsync(context).ConfigureAwait(false);

            return context;
        }

        async Task<MqttUnsubscriptionInterceptorContext> InterceptUnsubscribe(string topicFilter)
        {
            var interceptor = _options.UnsubscriptionInterceptor;
            if (interceptor == null)
            {
                return null;
            }

            var context = new MqttUnsubscriptionInterceptorContext
            {
                ClientId = _clientSession.ClientId,
                Topic = topicFilter,
                SessionItems = _clientSession.Items
            };

            await interceptor.InterceptUnsubscriptionAsync(context).ConfigureAwait(false);

            return context;
        }

        static MqttQualityOfServiceLevel GetEffectiveQoS(MqttQualityOfServiceLevel qosLevel, HashSet<MqttQualityOfServiceLevel> subscribedQoSLevels)
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

            return effectiveQoS;
        }
    }
}