using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;

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
                var finalTopicFilter = interceptorContext.TopicFilter;
                var acceptSubscription = interceptorContext.ReasonCode <= MqttSubscribeReasonCode.GrantedQoS2;

                if (string.IsNullOrEmpty(finalTopicFilter.Topic) || !acceptSubscription)
                {
                    // Return codes is for MQTT < 5 and Reason Code is for MQTT > 5.
                    result.ReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    result.ReasonCodes.Add(interceptorContext.ReasonCode);
                }
                else
                {
                    result.ReturnCodes.Add(interceptorContext.ReturnCode);
                    result.ReasonCodes.Add(interceptorContext.ReasonCode);
                }

                if (interceptorContext.CloseConnection)
                {
                    // When any of the interceptor calls leads to a connection close the connection
                    // must be closed. So do not revert to false!
                    result.CloseConnection = true;
                }
                
                if (!acceptSubscription || string.IsNullOrEmpty(finalTopicFilter.Topic))
                {
                    continue;
                }

                if (interceptorContext.ProcessSubscription)
                {
                    var subscription = CreateSubscription(
                        finalTopicFilter,
                        subscribePacket.Properties?.SubscriptionIdentifier ?? 0,
                        interceptorContext.ReasonCode);

                    await _eventDispatcher
                        .SafeNotifyClientSubscribedTopicAsync(_clientSession.ClientId, finalTopicFilter)
                        .ConfigureAwait(false);

                    FilterRetainedApplicationMessages(retainedApplicationMessages, subscription, result);
                }
            }

            return result;
        }

        public async Task<UnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var result = new UnsubscribeResult();

            _subscriptionsLock.EnterWriteLock();
            try
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscriptions.TryGetValue(topicFilter, out var existingSubscription);
                    
                    var interceptorContext = await InterceptUnsubscribe(topicFilter, existingSubscription).ConfigureAwait(false);
                    var acceptUnsubscription = interceptorContext.ReasonCode == MqttUnsubscribeReasonCode.Success;

                    result.ReasonCodes.Add(interceptorContext.ReasonCode);

                    if (interceptorContext.CloseConnection)
                    {
                        // When any of the interceptor calls leads to a connection close the connection
                        // must be closed. So do not revert to false!
                        result.CloseConnection = true;
                    }
           
                    if (!acceptUnsubscription)
                    {
                        continue;
                    }

                    if (interceptorContext.ProcessUnsubscription)
                    {
                        _subscriptions.Remove(topicFilter);
                    }
                }
            }
            finally
            {
                _subscriptionsLock.ExitWriteLock();
            }

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                await _eventDispatcher.SafeNotifyClientUnsubscribedTopicAsync(_clientSession.ClientId, topicFilter).ConfigureAwait(false);
            }

            return result;
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel,
            string senderClientId)
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

                if (MqttTopicFilterComparer.Compare(topic, subscription.Topic) != MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                qosLevels.Add(subscription.GrantedQualityOfServiceLevel);

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

        Subscription CreateSubscription(MqttTopicFilter topicFilter, uint subscriptionIdentifier,
            MqttSubscribeReasonCode reasonCode)
        {
            var subscription = new Subscription
            {
                Topic = topicFilter.Topic,
                NoLocal = topicFilter.NoLocal,
                RetainHandling = topicFilter.RetainHandling,
                RetainAsPublished = topicFilter.RetainAsPublished,
                Identifier = subscriptionIdentifier
            };

            if (reasonCode == MqttSubscribeReasonCode.GrantedQoS0)
            {
                subscription.GrantedQualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            }
            else if (reasonCode == MqttSubscribeReasonCode.GrantedQoS1)
            {
                subscription.GrantedQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            }
            else if (reasonCode == MqttSubscribeReasonCode.GrantedQoS2)
            {
                subscription.GrantedQualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            }
            else
            {
                throw new InvalidOperationException();
            }

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

        static void FilterRetainedApplicationMessages(IList<MqttApplicationMessage> retainedApplicationMessages,
            Subscription subscription, SubscribeResult subscribeResult)
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

                if (subscription.RetainHandling == MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly &&
                    !subscription.IsNewSubscription)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (MqttTopicFilterComparer.Compare(retainedApplicationMessage.Topic, subscription.Topic) !=
                    MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                var queuedApplicationMessage = new MqttQueuedApplicationMessage
                {
                    ApplicationMessage = retainedApplicationMessage,
                    IsRetainedMessage = true,
                    SubscriptionQualityOfServiceLevel = subscription.GrantedQualityOfServiceLevel
                };

                if (subscription.Identifier > 0)
                {
                    queuedApplicationMessage.SubscriptionIdentifiers = new List<uint> {subscription.Identifier};
                }

                subscribeResult.RetainedApplicationMessages.Add(queuedApplicationMessage);

                retainedApplicationMessages[i] = null;
            }
        }

        async Task<MqttSubscriptionInterceptorContext> InterceptSubscribe(MqttTopicFilter topicFilter)
        {
            var context = new MqttSubscriptionInterceptorContext
            {
                ClientId = _clientSession.ClientId,
                TopicFilter = topicFilter,
                SessionItems = _clientSession.Items,
                Session = new MqttSessionStatus(_clientSession)
            };

            if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                context.ReturnCode = MqttSubscribeReturnCode.SuccessMaximumQoS0;
                context.ReasonCode = MqttSubscribeReasonCode.GrantedQoS0;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                context.ReturnCode = MqttSubscribeReturnCode.SuccessMaximumQoS1;
                context.ReasonCode = MqttSubscribeReasonCode.GrantedQoS1;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                context.ReturnCode = MqttSubscribeReturnCode.SuccessMaximumQoS2;
                context.ReasonCode = MqttSubscribeReasonCode.GrantedQoS2;
            }

            context.DefaultReturnCode = context.ReturnCode;
            context.DefaultReasonCode = context.ReasonCode;

            if (topicFilter.Topic.StartsWith("$share/"))
            {
                context.ReasonCode = MqttSubscribeReasonCode.SharedSubscriptionsNotSupported;
                context.ReturnCode = MqttSubscribeReturnCode.Failure;
            }
            else
            {
                var interceptor = _options.SubscriptionInterceptor;
                if (interceptor != null)
                {
                    await interceptor.InterceptSubscriptionAsync(context).ConfigureAwait(false);
                }
            }

            return context;
        }

        async Task<MqttUnsubscriptionInterceptorContext> InterceptUnsubscribe(string topicFilter, Subscription subscription)
        {
            var context = new MqttUnsubscriptionInterceptorContext
            {
                ClientId = _clientSession.ClientId,
                Topic = topicFilter,
                SessionItems = _clientSession.Items
            };

            if (subscription == null)
            {
                context.DefaultReasonCode = MqttUnsubscribeReasonCode.NoSubscriptionExisted;
            }
            else
            {
                context.DefaultReasonCode = MqttUnsubscribeReasonCode.Success;
            }

            var interceptor = _options.UnsubscriptionInterceptor;
            if (interceptor == null)
            {
                return context;
            }

            await interceptor.InterceptUnsubscriptionAsync(context).ConfigureAwait(false);

            return context;
        }

        static MqttQualityOfServiceLevel GetEffectiveQoS(MqttQualityOfServiceLevel qosLevel,
            HashSet<MqttQualityOfServiceLevel> subscribedQoSLevels)
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