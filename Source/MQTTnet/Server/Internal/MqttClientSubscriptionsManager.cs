// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSubscriptionsManager : IDisposable
    {
        static readonly List<uint> EmptySubscriptionIdentifiers = new List<uint>();

        readonly MqttServerEventContainer _eventContainer;
        readonly Dictionary<ulong, HashSet<MqttSubscription>> _noWildcardSubscriptionsByTopicHash = new Dictionary<ulong, HashSet<MqttSubscription>>();
        readonly MqttRetainedMessagesManager _retainedMessagesManager;

        readonly MqttSession _session;

        // Callback to maintain list of subscriber clients
        readonly ISubscriptionChangedNotification _subscriptionChangedNotification;

        // Subscriptions are stored in various dictionaries and use a "topic hash"; see the MqttSubscription object for a detailed explanation.
        // The additional lock is important to coordinate complex update logic with multiple steps, checks and interceptors.
        readonly Dictionary<string, MqttSubscription> _subscriptions = new Dictionary<string, MqttSubscription>();

        // Use subscription lock to maintain consistency across subscriptions and topic hash dictionaries
        readonly SemaphoreSlim _subscriptionsLock = new SemaphoreSlim(1);
        readonly Dictionary<ulong, TopicHashMaskSubscriptions> _wildcardSubscriptionsByTopicHash = new Dictionary<ulong, TopicHashMaskSubscriptions>();

        public MqttClientSubscriptionsManager(
            MqttSession session,
            MqttServerEventContainer eventContainer,
            MqttRetainedMessagesManager retainedMessagesManager,
            ISubscriptionChangedNotification subscriptionChangedNotification)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _subscriptionChangedNotification = subscriptionChangedNotification;
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, ulong topicHash, MqttQualityOfServiceLevel applicationMessageQoSLevel, string senderClientId)
        {
            var possibleSubscriptions = new List<MqttSubscription>();

            // Check for possible subscriptions. They might have collisions but this is fine.
            _subscriptionsLock.Wait();
            try
            {
                if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var noWildcardSubscriptions))
                {
                    possibleSubscriptions.AddRange(noWildcardSubscriptions.ToList());
                }

                foreach (var wcs in _wildcardSubscriptionsByTopicHash)
                {
                    var wildcardSubscriptions = wcs.Value;
                    var subscriptionHash = wcs.Key;
                    var subscriptionHashMask = wildcardSubscriptions.HashMask;
                    
                    if ((topicHash & subscriptionHashMask) == subscriptionHash)
                    {
                        possibleSubscriptions.AddRange(wildcardSubscriptions.Subscriptions.ToList());
                    }
                }
            }
            finally
            {
                _subscriptionsLock.Release();
            }
            
            // The pre check has evaluated that nothing is subscribed.
            // If there were some possible candidates they get checked below
            // again to avoid collisions.
            if (possibleSubscriptions.Count == 0)
            {
                return CheckSubscriptionsResult.NotSubscribed;
            }

            var senderIsReceiver = string.Equals(senderClientId, _session.Id);
            var maxQoSLevel = -1; // Not subscribed.

            HashSet<uint> subscriptionIdentifiers = null;
            var retainAsPublished = false;

            foreach (var subscription in possibleSubscriptions)
            {
                if (subscription.NoLocal && senderIsReceiver)
                {
                    // This is a MQTTv5 feature!
                    continue;
                }

                if (MqttTopicFilterComparer.Compare(topic, subscription.Topic) != MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                if (subscription.RetainAsPublished)
                {
                    // This is a MQTTv5 feature!
                    retainAsPublished = true;
                }

                if ((int)subscription.GrantedQualityOfServiceLevel > maxQoSLevel)
                {
                    maxQoSLevel = (int)subscription.GrantedQualityOfServiceLevel;
                }

                if (subscription.Identifier > 0)
                {
                    if (subscriptionIdentifiers == null)
                    {
                        subscriptionIdentifiers = new HashSet<uint>();
                    }

                    subscriptionIdentifiers.Add(subscription.Identifier);
                }
            }

            if (maxQoSLevel == -1)
            {
                return CheckSubscriptionsResult.NotSubscribed;
            }

            var result = new CheckSubscriptionsResult
            {
                IsSubscribed = true,
                RetainAsPublished = retainAsPublished,
                SubscriptionIdentifiers = subscriptionIdentifiers?.ToList() ?? EmptySubscriptionIdentifiers,

                // Start with the same QoS as the publisher.
                QualityOfServiceLevel = applicationMessageQoSLevel
            };

            // Now downgrade if required.
            //
            // If a subscribing Client has been granted maximum QoS 1 for a particular Topic Filter, then a QoS 0 Application Message matching the filter is delivered
            // to the Client at QoS 0. This means that at most one copy of the message is received by the Client. On the other hand, a QoS 2 Message published to
            // the same topic is downgraded by the Server to QoS 1 for delivery to the Client, so that Client might receive duplicate copies of the Message.

            // Subscribing to a Topic Filter at QoS 2 is equivalent to saying "I would like to receive Messages matching this filter at the QoS with which they were published".
            // This means a publisher is responsible for determining the maximum QoS a Message can be delivered at, but a subscriber is able to require that the Server
            // downgrades the QoS to one more suitable for its usage.
            if (maxQoSLevel < (int)applicationMessageQoSLevel)
            {
                result.QualityOfServiceLevel = (MqttQualityOfServiceLevel)maxQoSLevel;
            }

            return result;
        }

        public void Dispose()
        {
            _subscriptionsLock.Dispose();
        }

        public async Task<SubscribeResult> Subscribe(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            if (subscribePacket == null)
            {
                throw new ArgumentNullException(nameof(subscribePacket));
            }

            var retainedApplicationMessages = await _retainedMessagesManager.GetMessages().ConfigureAwait(false);
            var result = new SubscribeResult
            {
                ReasonCodes = new List<MqttSubscribeReasonCode>(subscribePacket.TopicFilters.Count)
            };

            var addedSubscriptions = new List<string>();

            // The topic filters are order by its QoS so that the higher QoS will win over a
            // lower one.
            foreach (var originalTopicFilter in subscribePacket.TopicFilters.OrderByDescending(f => f.QualityOfServiceLevel))
            {
                var subscriptionEventArgs = await InterceptSubscribe(originalTopicFilter, cancellationToken).ConfigureAwait(false);
                var finalTopicFilter = subscriptionEventArgs.TopicFilter;
                var processSubscription = subscriptionEventArgs.ProcessSubscription && subscriptionEventArgs.Response.ReasonCode <= MqttSubscribeReasonCode.GrantedQoS2;

                result.UserProperties = subscriptionEventArgs.UserProperties;
                result.ReasonString = subscriptionEventArgs.ReasonString;
                result.ReasonCodes.Add(subscriptionEventArgs.Response.ReasonCode);

                if (subscriptionEventArgs.CloseConnection)
                {
                    // When any of the interceptor calls leads to a connection close the connection
                    // must be closed. So do not revert to false!
                    result.CloseConnection = true;
                }

                if (!processSubscription || string.IsNullOrEmpty(finalTopicFilter.Topic))
                {
                    continue;
                }

                var createSubscriptionResult = CreateSubscription(finalTopicFilter, subscribePacket.SubscriptionIdentifier, subscriptionEventArgs.Response.ReasonCode);

                addedSubscriptions.Add(finalTopicFilter.Topic);

                if (_eventContainer.ClientSubscribedTopicEvent.HasHandlers)
                {
                    var eventArgs = new ClientSubscribedTopicEventArgs
                    {
                        ClientId = _session.Id,
                        TopicFilter = finalTopicFilter
                    };

                    await _eventContainer.ClientSubscribedTopicEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }

                FilterRetainedApplicationMessages(retainedApplicationMessages, createSubscriptionResult, result);
            }

            _subscriptionChangedNotification?.OnSubscriptionsAdded(_session, addedSubscriptions);

            return result;
        }

        public async Task<MqttUnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            if (unsubscribePacket == null)
            {
                throw new ArgumentNullException(nameof(unsubscribePacket));
            }

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
                        _subscriptions.Remove(topicFilter);

                        // must remove subscription object from topic hash dictionary also

                        if (existingSubscription.TopicHasWildcard)
                        {
                            if (_wildcardSubscriptionsByTopicHash.TryGetValue(existingSubscription.TopicHash, out var subs))
                            {
                                subs.Subscriptions.Remove(existingSubscription);
                                if (subs.Subscriptions.Count == 0)
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
                                    _noWildcardSubscriptionsByTopicHash.Remove(existingSubscription.TopicHash);
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

            if (_eventContainer.ClientUnsubscribedTopicEvent.HasHandlers)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    var eventArgs = new ClientUnsubscribedTopicEventArgs
                    {
                        ClientId = _session.Id,
                        TopicFilter = topicFilter
                    };

                    await _eventContainer.ClientUnsubscribedTopicEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }
            }

            return result;
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
                subscriptionIdentifier);

            bool isNewSubscription;

            // Add to subscriptions and maintain topic hash dictionaries

            _subscriptionsLock.Wait();
            try
            {
                MqttSubscription.CalculateTopicHash(topicFilter.Topic, out var topicHash, out var topicHashMask, out var hasWildcard);

                if (_subscriptions.TryGetValue(topicFilter.Topic, out var existingSubscription))
                {
                    // must remove object from topic hash dictionary first
                    if (hasWildcard)
                    {
                        if (_wildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subs))
                        {
                            subs.Subscriptions.Remove(existingSubscription);
                            // no need to remove empty entry because we'll be adding subscription again below
                        }
                    }
                    else
                    {
                        if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subscriptions))
                        {
                            subscriptions.Remove(existingSubscription);
                            // no need to remove empty entry because we'll be adding subscription again below
                        }
                    }
                }

                isNewSubscription = existingSubscription == null;
                _subscriptions[topicFilter.Topic] = subscription;

                // Add or re-add to topic hash dictionary
                if (hasWildcard)
                {
                    if (!_wildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subscriptions))
                    {
                        subscriptions = new TopicHashMaskSubscriptions(topicHashMask);
                        _wildcardSubscriptionsByTopicHash.Add(topicHash, subscriptions);
                    }

                    subscriptions.Subscriptions.Add(subscription);
                }
                else
                {
                    if (!_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subscriptions))
                    {
                        subscriptions = new HashSet<MqttSubscription>();
                        _noWildcardSubscriptionsByTopicHash.Add(topicHash, subscriptions);
                    }

                    subscriptions.Add(subscription);
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

        static void FilterRetainedApplicationMessages(
            IList<MqttApplicationMessage> retainedApplicationMessages,
            CreateSubscriptionResult createSubscriptionResult,
            SubscribeResult subscribeResult)
        {
            for (var index = retainedApplicationMessages.Count - 1; index >= 0; index--)
            {
                var retainedApplicationMessage = retainedApplicationMessages[index];
                if (retainedApplicationMessage == null)
                {
                    continue;
                }

                if (createSubscriptionResult.Subscription.RetainHandling == MqttRetainHandling.DoNotSendOnSubscribe)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (createSubscriptionResult.Subscription.RetainHandling == MqttRetainHandling.SendAtSubscribeIfNewSubscriptionOnly && !createSubscriptionResult.IsNewSubscription)
                {
                    // This is a MQTT V5+ feature.
                    continue;
                }

                if (MqttTopicFilterComparer.Compare(retainedApplicationMessage.Topic, createSubscriptionResult.Subscription.Topic) != MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                var retainedMessageMatch = new MqttRetainedMessageMatch
                {
                    ApplicationMessage = retainedApplicationMessage,
                    SubscriptionQualityOfServiceLevel = createSubscriptionResult.Subscription.GrantedQualityOfServiceLevel
                };

                if (subscribeResult.RetainedMessages == null)
                {
                    subscribeResult.RetainedMessages = new List<MqttRetainedMessageMatch>();
                }

                subscribeResult.RetainedMessages.Add(retainedMessageMatch);

                // Clear the retained message from the list because the client should receive every message only 
                // one time even if multiple subscriptions affect them.
                retainedApplicationMessages[index] = null;
            }
        }

        async Task<InterceptingSubscriptionEventArgs> InterceptSubscribe(MqttTopicFilter topicFilter, CancellationToken cancellationToken)
        {
            var eventArgs = new InterceptingSubscriptionEventArgs
            {
                ClientId = _session.Id,
                TopicFilter = topicFilter,
                SessionItems = _session.Items,
                Session = new MqttSessionStatus(_session),
                CancellationToken = cancellationToken
            };

            if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                eventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS0;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                eventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS1;
            }
            else if (topicFilter.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                eventArgs.Response.ReasonCode = MqttSubscribeReasonCode.GrantedQoS2;
            }

            if (topicFilter.Topic.StartsWith("$share/"))
            {
                eventArgs.Response.ReasonCode = MqttSubscribeReasonCode.SharedSubscriptionsNotSupported;
            }
            else
            {
                await _eventContainer.InterceptingSubscriptionEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }

            return eventArgs;
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

        public sealed class CreateSubscriptionResult
        {
            public bool IsNewSubscription { get; set; }

            public MqttSubscription Subscription { get; set; }
        }
    }
}