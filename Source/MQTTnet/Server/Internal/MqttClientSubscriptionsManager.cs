using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager
    {
        // We use a reader writer lock because the subscriptions are only read most of the time.
        // Since writing is done by multiple threads (server API or connection thread), we cannot avoid locking
        // completely by swapping references etc.
        readonly SemaphoreSlim _subscriptionsLock = new SemaphoreSlim(1);
        
        // The subscriptions are stored as a ConcurrentDictionary in order ensure that reading the data is save.
        // The additional lock is important to coordinate complex update logic with multiple steps, checks and interceptors.
        readonly ConcurrentDictionary<string, MqttSubscription> _subscriptions = new ConcurrentDictionary<string, MqttSubscription>();
        readonly Dictionary<ulong, HashSet<MqttSubscription>> _noWildcardSubscriptionsByTopicHash = new Dictionary<ulong, HashSet<MqttSubscription>>();
        readonly Dictionary<ulong, HashSet<MqttSubscription>> _wildcardSubscriptionsByTopicHash = new Dictionary<ulong, HashSet<MqttSubscription>>();

        readonly MqttSession _session;
        readonly MqttServerEventContainer _eventContainer;
        readonly MqttRetainedMessagesManager _retainedMessagesManager;
        readonly ISubscriptionChangedNotification _subscriptionChangedNotification;

        public MqttClientSubscriptionsManager(
            MqttSession session,
            MqttServerEventContainer eventContainer,
            MqttRetainedMessagesManager retainedMessagesManager,
            ISubscriptionChangedNotification subscriptionChangedNotification
            )
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _subscriptionChangedNotification = subscriptionChangedNotification;
        }

        public async Task<SubscribeResult> Subscribe(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var retainedApplicationMessages = await _retainedMessagesManager.GetMessages().ConfigureAwait(false);
            var result = new SubscribeResult();

            var addedSubscriptions = new List<string>();

            // The topic filters are order by its QoS so that the higher QoS will win over a
            // lower one.
            foreach (var originalTopicFilter in subscribePacket.TopicFilters.OrderByDescending(f => f.QualityOfServiceLevel))
            {
                var interceptorContext = await InterceptSubscribe(originalTopicFilter, cancellationToken).ConfigureAwait(false);
                var finalTopicFilter = interceptorContext.TopicFilter;
                var processSubscription = interceptorContext.Response.ReasonCode <= MqttSubscribeReasonCode.GrantedQoS2;

                if (string.IsNullOrEmpty(finalTopicFilter.Topic) || !processSubscription)
                {
                    // Return codes is for MQTT < 5 and Reason Code is for MQTT > 5.
                    result.ReturnCodes.Add(MqttSubscribeReturnCode.Failure);
                    result.ReasonCodes.Add(interceptorContext.Response.ReasonCode);
                }
                else
                {
                    result.ReturnCodes.Add((MqttSubscribeReturnCode) interceptorContext.Response.ReasonCode);
                    result.ReasonCodes.Add(interceptorContext.Response.ReasonCode);
                }

                if (interceptorContext.CloseConnection)
                {
                    // When any of the interceptor calls leads to a connection close the connection
                    // must be closed. So do not revert to false!
                    result.CloseConnection = true;
                }

                if (!processSubscription || string.IsNullOrEmpty(finalTopicFilter.Topic))
                {
                    continue;
                }

                if (interceptorContext.ProcessSubscription)
                {
                    var createSubscriptionResult = CreateSubscription(
                        finalTopicFilter,
                        subscribePacket.Properties?.SubscriptionIdentifier ?? 0,
                        interceptorContext.Response.ReasonCode);

                    addedSubscriptions.Add(finalTopicFilter.Topic);

                    await _eventContainer.ClientSubscribedTopicEvent.InvokeAsync(() => new ClientSubscribedTopicEventArgs
                    {
                        ClientId = _session.Id,
                        TopicFilter = finalTopicFilter
                    }).ConfigureAwait(false);

                    FilterRetainedApplicationMessages(retainedApplicationMessages, createSubscriptionResult, result);
                }
            }

            if (_subscriptionChangedNotification != null)
            {
                _subscriptionChangedNotification.OnSubscriptionsAdded(_session, addedSubscriptions);
            }

            return result;
        }

        public async Task<MqttUnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var result = new MqttUnsubscribeResult();

            var removedSubscriptions = new List<string>();

            await _subscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    _subscriptions.TryGetValue(topicFilter, out var existingSubscription);

                    var interceptorContext = await InterceptUnsubscribe(topicFilter, existingSubscription, cancellationToken).ConfigureAwait(false);
                    var acceptUnsubscription = interceptorContext.Response.ReasonCode == MqttUnsubscribeReasonCode.Success;

                    result.ReasonCodes.Add(interceptorContext.Response.ReasonCode);

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
                        _subscriptions.TryRemove(topicFilter, out _);

                        // must remove subscription object from topic hash dictionary also
                        
                        if (existingSubscription.TopicHasWildcard)
                        {
                            if (_wildcardSubscriptionsByTopicHash.TryGetValue(existingSubscription.TopicHash, out var subs))
                            {
                                subs.Remove(existingSubscription);
                                if (subs.Count == 0)
                                {
                                    _wildcardSubscriptionsByTopicHash.Remove(existingSubscription.TopicHash);
                                }
                            }
                        }
                        else
                        {
                            if (_noWildcardSubscriptionsByTopicHash.TryGetValue(existingSubscription.TopicHash, out var subs))
                            {
                                subs.Remove(existingSubscription);
                                if (subs.Count == 0)
                                {
                                    _wildcardSubscriptionsByTopicHash.Remove(existingSubscription.TopicHash);
                                }
                            }
                        }

                        removedSubscriptions.Add(topicFilter);
                    }
                }
            }
            finally
            {
                _subscriptionsLock.Release();

                if (_subscriptionChangedNotification != null)
                {
                    _subscriptionChangedNotification.OnSubscriptionsRemoved(_session, removedSubscriptions);
                }
            }

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                await _eventContainer.ClientUnsubscribedTopicEvent.InvokeAsync(() => new ClientUnsubscribedTopicEventArgs
                {
                    ClientId = _session.Id,
                    TopicFilter = topicFilter
                }).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Note: Use topic hash based method for repeated calls.
        /// </summary>
        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel, string senderClientId)
        {
            ulong topicHash;
            ulong topicHashMask; // not needed
            bool hasWildcard;    // not needed
            MqttSubscription.CalcTopicHash(topic, out topicHash, out topicHashMask, out hasWildcard);
            return CheckSubscriptions(topic, topicHash, qosLevel, senderClientId);
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, ulong topicHash, MqttQualityOfServiceLevel qosLevel, string senderClientId)
        {
            List<MqttSubscription> subscriptions = new List<MqttSubscription>();

            _subscriptionsLock.Wait();
            try
            {
                if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var noWildcardSubs))
                {
                    subscriptions.AddRange(noWildcardSubs.ToList());
                }

                if (_wildcardSubscriptionsByTopicHash.Count > 0)
                {
                    // Later improvement:
                    // Implement a binary tree to hold wildcard topics, lookup the first key (subscribed topic hash)
                    // that is greater or equal than the application message topic hash, then traverse through tree nodes
                    // until first non-matching topic hash is found.
                    foreach (var wcs in _wildcardSubscriptionsByTopicHash)
                    {
                        var subscriptionHash = wcs.Key;
                        var wildcardSubs = wcs.Value;
                        // All subscription with the same topic hash have the same topic hash mask; take the first one.
                        // Note: empty hash sets are removed and the wildcardsSubs contains at least one element.
                        // Note: Using First() via Linq seems to be slower than entering the loop and existing after first check.
                        foreach (var wc in wildcardSubs)
                        {
                            var subscriptionHashMask = wc.TopicHashMask;
                            if ((topicHash & subscriptionHashMask) == subscriptionHash)
                            {
                                subscriptions.AddRange(wildcardSubs.ToList());
                            }
                            break;
                        }
                    }
                }

                if (subscriptions.Count == 0)
                {
                    return CheckSubscriptionsResult.NotSubscribed;
                }
            }
            finally
            {
                _subscriptionsLock.Release();
            }

            var senderIsReceiver = string.Equals(senderClientId, _session.Id);

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

        public sealed class CreateSubscriptionResult
        {
            public MqttSubscription Subscription { get; set; }

            public bool IsNewSubscription { get; set; }
        }

        CreateSubscriptionResult CreateSubscription(MqttTopicFilter topicFilter, uint subscriptionIdentifier, MqttSubscribeReasonCode reasonCode)
        {
            MqttQualityOfServiceLevel grantedQualityOfServiceLevel;

            if (reasonCode == MqttSubscribeReasonCode.GrantedQoS0)
            {
                grantedQualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
            }
            else if (reasonCode == MqttSubscribeReasonCode.GrantedQoS1)
            {
                grantedQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
            }
            else if (reasonCode == MqttSubscribeReasonCode.GrantedQoS2)
            {
                grantedQualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
            }
            else
            {
                throw new InvalidOperationException();
            }

            var subscription = new MqttSubscription(
                topicFilter.Topic,
                topicFilter.NoLocal,
                topicFilter.RetainHandling,
                topicFilter.RetainAsPublished,
                grantedQualityOfServiceLevel,
                subscriptionIdentifier
            );

            bool isNewSubscription;

            _subscriptionsLock.Wait();
            try
            {
                ulong topicHash;
                ulong topicHashMask;
                bool hasWildcard;
                MqttSubscription.CalcTopicHash(topicFilter.Topic, out topicHash, out topicHashMask, out hasWildcard);

                if (_subscriptions.TryGetValue(topicFilter.Topic, out var existingSubscription))
                {
                    // must remove object from topic hash dictionary first
                    if (hasWildcard)
                    {
                        if (_wildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subs))
                        {
                            subs.Remove(existingSubscription);
                            // no need to remove empty entry because we'll be adding subscription again below
                        }
                    }
                    else
                    {
                        if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subs))
                        {
                            subs.Remove(existingSubscription);
                            // no need to remove empty entry because we'll be adding subscription again below
                        }
                    }
                }

                isNewSubscription = existingSubscription == null;
                _subscriptions[topicFilter.Topic] = subscription;

                // Add or re-add to topic hash dictionary
                if (hasWildcard)
                {
                    if (!_wildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subs))
                    {
                        subs = new HashSet<MqttSubscription>();
                        _wildcardSubscriptionsByTopicHash.Add(topicHash, subs);
                    }
                    subs.Add(subscription);
                }
                else
                {
                    if (!_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subs))
                    {
                        subs = new HashSet<MqttSubscription>();
                        _noWildcardSubscriptionsByTopicHash.Add(topicHash, subs);
                    }
                    subs.Add(subscription);
                }
            }
            finally
            {
                _subscriptionsLock.Release();
            }

            return new CreateSubscriptionResult
            {
                IsNewSubscription = isNewSubscription,
                Subscription = subscription
            };
        }

        static void FilterRetainedApplicationMessages(IList<MqttApplicationMessage> retainedApplicationMessages, CreateSubscriptionResult createSubscriptionResult,
            SubscribeResult subscribeResult)
        {
            for (var i = retainedApplicationMessages.Count - 1; i >= 0; i--)
            {
                var retainedApplicationMessage = retainedApplicationMessages[i];
                if (retainedApplicationMessage == null)
                {
                    continue;
                }

                if (createSubscriptionResult.Subscription.RetainHandling == MqttRetainHandling.DoNotSendOnSubscribe)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (createSubscriptionResult.Subscription.RetainHandling == MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly &&
                    !createSubscriptionResult.IsNewSubscription)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (MqttTopicFilterComparer.Compare(retainedApplicationMessage.Topic, createSubscriptionResult.Subscription.Topic) !=
                    MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                var queuedApplicationMessage = new MqttQueuedApplicationMessage
                {
                    ApplicationMessage = retainedApplicationMessage,
                    SubscriptionQualityOfServiceLevel = createSubscriptionResult.Subscription.GrantedQualityOfServiceLevel
                };

                // if (createSubscriptionResult.Subscription.Identifier > 0)
                // {
                //     queuedApplicationMessage.SubscriptionIdentifiers = new List<uint> {createSubscriptionResult.Subscription.Identifier};
                // }

                subscribeResult.RetainedApplicationMessages.Add(queuedApplicationMessage);

                retainedApplicationMessages[i] = null;
            }
        }

        async Task<InterceptingSubscriptionEventArgs> InterceptSubscribe(MqttTopicFilter topicFilter, CancellationToken cancellationToken)
        {
            var subscriptionReceivedEventArgs = new InterceptingSubscriptionEventArgs
            {
                ClientId = _session.Id,
                TopicFilter = topicFilter,
                SessionItems = _session.Items,
                Session = new MqttSessionStatus(_session),
                CancellationToken = cancellationToken
            };

            if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                subscriptionReceivedEventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS0;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                subscriptionReceivedEventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS1;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                subscriptionReceivedEventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS2;
            }

            if (topicFilter.Topic.StartsWith("$share/"))
            {
                subscriptionReceivedEventArgs.Response.ReasonCode = MqttSubscribeReasonCode.SharedSubscriptionsNotSupported;
            }
            else
            {
                await _eventContainer.InterceptingSubscriptionEvent.InvokeAsync(subscriptionReceivedEventArgs).ConfigureAwait(false);
            }

            return subscriptionReceivedEventArgs;
        }

        async Task<InterceptingUnsubscriptionEventArgs> InterceptUnsubscribe(string topicFilter, MqttSubscription mqttSubscription, CancellationToken cancellationToken)
        {
            var clientUnsubscribingTopicEventArgs = new InterceptingUnsubscriptionEventArgs
            {
                ClientId = _session.Id,
                Topic = topicFilter,
                SessionItems = _session.Items,
                CancellationToken = cancellationToken
            };

            if (mqttSubscription == null)
            {
                clientUnsubscribingTopicEventArgs.Response.ReasonCode = MqttUnsubscribeReasonCode.NoSubscriptionExisted;
            }
            else
            {
                clientUnsubscribingTopicEventArgs.Response.ReasonCode = MqttUnsubscribeReasonCode.Success;
            }

            await _eventContainer.InterceptingUnsubscriptionEvent.InvokeAsync(clientUnsubscribingTopicEventArgs).ConfigureAwait(false);

            return clientUnsubscribingTopicEventArgs;
        }

        static MqttQualityOfServiceLevel GetEffectiveQoS(MqttQualityOfServiceLevel qosLevel, ICollection<MqttQualityOfServiceLevel> subscribedQoSLevels)
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