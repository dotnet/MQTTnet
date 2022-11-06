// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;
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
        readonly AsyncLock _subscriptionsLock = new AsyncLock();
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

        public CheckSubscriptionsResult CheckSubscriptions(string topic, ulong topicHash, MqttQualityOfServiceLevel qualityOfServiceLevel, string senderId)
        {
            var possibleSubscriptions = new List<MqttSubscription>();

            // Check for possible subscriptions. They might have collisions but this is fine.
            using (_subscriptionsLock.EnterAsync(CancellationToken.None).GetAwaiter().GetResult())
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

            // The pre check has evaluated that nothing is subscribed.
            // If there were some possible candidates they get checked below
            // again to avoid collisions.
            if (possibleSubscriptions.Count == 0)
            {
                return CheckSubscriptionsResult.NotSubscribed;
            }

            var senderIsReceiver = string.Equals(senderId, _session.Id);
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
                QualityOfServiceLevel = qualityOfServiceLevel
            };

            // Now downgrade if required.
            //
            // If a subscribing Client has been granted maximum QoS 1 for a particular Topic Filter, then a QoS 0 Application Message matching the filter is delivered
            // to the Client at QoS 0. This means that at most one copy of the message is received by the Client. On the other hand, a QoS 2 Message published to
            // the same topic is downgraded by the Server to QoS 1 for delivery to the Client, so that Client might receive duplicate copies of the Message.

            // Subscribing to a Topic Filter at QoS 2 is equivalent to saying "I would like to receive Messages matching this filter at the QoS with which they were published".
            // This means a publisher is responsible for determining the maximum QoS a Message can be delivered at, but a subscriber is able to require that the Server
            // downgrades the QoS to one more suitable for its usage.
            if (maxQoSLevel < (int)qualityOfServiceLevel)
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
            var result = new SubscribeResult(subscribePacket.TopicFilters.Count);

            var addedSubscriptions = new List<string>();
            var finalTopicFilters = new List<MqttTopicFilter>();

            // The topic filters are order by its QoS so that the higher QoS will win over a
            // lower one.
            foreach (var topicFilterItem in subscribePacket.TopicFilters.OrderByDescending(f => f.QualityOfServiceLevel))
            {
                var subscriptionEventArgs = await InterceptSubscribe(topicFilterItem, cancellationToken).ConfigureAwait(false);
                var topicFilter = subscriptionEventArgs.TopicFilter;
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

                if (!processSubscription || string.IsNullOrEmpty(topicFilter.Topic))
                {
                    continue;
                }

                var createSubscriptionResult = CreateSubscription(topicFilter, subscribePacket.SubscriptionIdentifier, subscriptionEventArgs.Response.ReasonCode);

                addedSubscriptions.Add(topicFilter.Topic);
                finalTopicFilters.Add(topicFilter);

                FilterRetainedApplicationMessages(retainedApplicationMessages, createSubscriptionResult, result);
            }

            // This call will add the new subscription to the internal storage.
            // So the event _ClientSubscribedTopicEvent_ must be called afterwards.
            _subscriptionChangedNotification?.OnSubscriptionsAdded(_session, addedSubscriptions);

            if (_eventContainer.ClientSubscribedTopicEvent.HasHandlers)
            {
                foreach (var finalTopicFilter in finalTopicFilters)
                {
                    var eventArgs = new ClientSubscribedTopicEventArgs(_session.Id, finalTopicFilter, _session.Items);
                    await _eventContainer.ClientSubscribedTopicEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }
            }

            return result;
        }

        public async Task<UnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            if (unsubscribePacket == null)
            {
                throw new ArgumentNullException(nameof(unsubscribePacket));
            }

            var result = new UnsubscribeResult();

            var removedSubscriptions = new List<string>();

            using (await _subscriptionsLock.EnterAsync(cancellationToken).ConfigureAwait(false))
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
                        if (existingSubscription != null)
                        {
                            var topicHash = existingSubscription.TopicHash;

                            if (existingSubscription.TopicHasWildcard)
                            {
                                if (_wildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subscriptions))
                                {
                                    subscriptions.Subscriptions.Remove(existingSubscription);
                                    if (subscriptions.Subscriptions.Count == 0)
                                    {
                                        _wildcardSubscriptionsByTopicHash.Remove(topicHash);
                                    }
                                }
                            }
                            else
                            {
                                if (_noWildcardSubscriptionsByTopicHash.TryGetValue(topicHash, out var subscriptions))
                                {
                                    subscriptions.Remove(existingSubscription);
                                    if (subscriptions.Count == 0)
                                    {
                                        _noWildcardSubscriptionsByTopicHash.Remove(topicHash);
                                    }
                                }
                            }
                        }

                        removedSubscriptions.Add(topicFilter);
                    }
                }
            }

            _subscriptionChangedNotification?.OnSubscriptionsRemoved(_session, removedSubscriptions);

            if (_eventContainer.ClientUnsubscribedTopicEvent.HasHandlers)
            {
                foreach (var topicFilter in unsubscribePacket.TopicFilters)
                {
                    var eventArgs = new ClientUnsubscribedTopicEventArgs(_session.Id, topicFilter, _session.Items);
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

            using (_subscriptionsLock.EnterAsync(CancellationToken.None).GetAwaiter().GetResult())
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

            return new CreateSubscriptionResult
            {
                IsNewSubscription = isNewSubscription,
                Subscription = subscription
            };
        }

        static void FilterRetainedApplicationMessages(
            IList<MqttApplicationMessage> retainedMessages,
            CreateSubscriptionResult createSubscriptionResult,
            SubscribeResult subscribeResult)
        {
            for (var index = retainedMessages.Count - 1; index >= 0; index--)
            {
                var retainedMessage = retainedMessages[index];
                if (retainedMessage == null)
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

                if (MqttTopicFilterComparer.Compare(retainedMessage.Topic, createSubscriptionResult.Subscription.Topic) != MqttTopicFilterCompareResult.IsMatch)
                {
                    continue;
                }

                var retainedMessageMatch = new MqttRetainedMessageMatch(retainedMessage, createSubscriptionResult.Subscription.GrantedQualityOfServiceLevel);
                if (retainedMessageMatch.SubscriptionQualityOfServiceLevel > retainedMessageMatch.ApplicationMessage.QualityOfServiceLevel)
                {
                    // UPGRADING the QoS is not allowed! 
                    // From MQTT spec: Subscribing to a Topic Filter at QoS 2 is equivalent to saying
                    // "I would like to receive Messages matching this filter at the QoS with which they were published".
                    // This means a publisher is responsible for determining the maximum QoS a Message can be delivered at,
                    // but a subscriber is able to require that the Server downgrades the QoS to one more suitable for its usage.
                    retainedMessageMatch.SubscriptionQualityOfServiceLevel = retainedMessageMatch.ApplicationMessage.QualityOfServiceLevel;
                }

                if (subscribeResult.RetainedMessages == null)
                {
                    subscribeResult.RetainedMessages = new List<MqttRetainedMessageMatch>();
                }

                subscribeResult.RetainedMessages.Add(retainedMessageMatch);

                // Clear the retained message from the list because the client should receive every message only 
                // one time even if multiple subscriptions affect them.
                retainedMessages[index] = null;
            }
        }

        async Task<InterceptingSubscriptionEventArgs> InterceptSubscribe(MqttTopicFilter topicFilter, CancellationToken cancellationToken)
        {
            var eventArgs = new InterceptingSubscriptionEventArgs(cancellationToken, _session.Id, new MqttSessionStatus(_session), topicFilter);

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
            var clientUnsubscribingTopicEventArgs = new InterceptingUnsubscriptionEventArgs(cancellationToken, topicFilter, _session.Items, topicFilter)
            {
                Response =
                {
                    ReasonCode = mqttSubscription == null ? MqttUnsubscribeReasonCode.NoSubscriptionExisted : MqttUnsubscribeReasonCode.Success
                }
            };

            await _eventContainer.InterceptingUnsubscriptionEvent.InvokeAsync(clientUnsubscribingTopicEventArgs).ConfigureAwait(false);

            return clientUnsubscribingTopicEventArgs;
        }

        sealed class CreateSubscriptionResult
        {
            public bool IsNewSubscription { get; set; }

            public MqttSubscription Subscription { get; set; }
        }
    }
}