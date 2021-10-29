using System;
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
        readonly ReaderWriterLockSlim _subscriptionsLock = new ReaderWriterLockSlim();
        readonly Dictionary<string, MqttSubscription> _subscriptions = new Dictionary<string, MqttSubscription>(4096);

        readonly MqttClientSession _clientSession;
        readonly MqttServerOptions _options;
        readonly MqttServerEventContainer _eventContainer;
        readonly IMqttRetainedMessagesManager _retainedMessagesManager;

        public MqttClientSubscriptionsManager(
            MqttClientSession clientSession,
            MqttServerOptions serverOptions,
            MqttServerEventContainer eventContainer,
            IMqttRetainedMessagesManager retainedMessagesManager)
        {
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));
            _options = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public async Task<MqttSubscribeResult> Subscribe(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));

            var retainedApplicationMessages = await _retainedMessagesManager.GetMessagesAsync().ConfigureAwait(false);
            var result = new MqttSubscribeResult();

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

                    await _eventContainer.ClientSubscribedTopicEvent.InvokeAsync(() => new MqttServerClientSubscribedTopicEventArgs
                    {
                        ClientId = _clientSession.Id,
                        TopicFilter = finalTopicFilter
                    }).ConfigureAwait(false);

                    FilterRetainedApplicationMessages(retainedApplicationMessages, createSubscriptionResult, result);
                }
            }

            return result;
        }

        public async Task<MqttUnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));

            var result = new MqttUnsubscribeResult();

            _subscriptionsLock.EnterWriteLock();
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
                    }
                }
            }
            finally
            {
                _subscriptionsLock.ExitWriteLock();
            }

            foreach (var topicFilter in unsubscribePacket.TopicFilters)
            {
                await _eventContainer.ClientUnsubscribedTopicEvent.InvokeAsync(() => new MqttServerClientUnsubscribedTopicEventArgs
                {
                    ClientId = _clientSession.Id,
                    TopicFilter = topicFilter
                }).ConfigureAwait(false);
            }

            return result;
        }

        public CheckSubscriptionsResult CheckSubscriptions(string topic, MqttQualityOfServiceLevel qosLevel,
            string senderClientId)
        {
            List<MqttSubscription> subscriptions;
            _subscriptionsLock.EnterReadLock();
            try
            {
                subscriptions = _subscriptions.Values.ToList();
            }
            finally
            {
                _subscriptionsLock.ExitReadLock();
            }

            var senderIsReceiver = string.Equals(senderClientId, _clientSession.Id);

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
            var subscription = new MqttSubscription
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

            bool isNewSubscription;

            _subscriptionsLock.EnterWriteLock();
            try
            {
                isNewSubscription = !_subscriptions.ContainsKey(topicFilter.Topic);
                _subscriptions[topicFilter.Topic] = subscription;
            }
            finally
            {
                _subscriptionsLock.ExitWriteLock();
            }

            return new CreateSubscriptionResult
            {
                IsNewSubscription = isNewSubscription,
                Subscription = subscription
            };
        }

        static void FilterRetainedApplicationMessages(IList<MqttApplicationMessage> retainedApplicationMessages, CreateSubscriptionResult createSubscriptionResult,
            MqttSubscribeResult subscribeResult)
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
                    IsRetainedMessage = true,
                    SubscriptionQualityOfServiceLevel = createSubscriptionResult.Subscription.GrantedQualityOfServiceLevel
                };

                if (createSubscriptionResult.Subscription.Identifier > 0)
                {
                    queuedApplicationMessage.SubscriptionIdentifiers = new List<uint> {createSubscriptionResult.Subscription.Identifier};
                }

                subscribeResult.RetainedApplicationMessages.Add(queuedApplicationMessage);

                retainedApplicationMessages[i] = null;
            }
        }

        async Task<InterceptingMqttClientSubscriptionEventArgs> InterceptSubscribe(MqttTopicFilter topicFilter, CancellationToken cancellationToken)
        {
            var subscriptionReceivedEventArgs = new InterceptingMqttClientSubscriptionEventArgs
            {
                ClientId = _clientSession.Id,
                TopicFilter = topicFilter,
                SessionItems = _clientSession.Items,
                Session = new MqttSessionStatus(_clientSession),
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
                await _eventContainer.InterceptingClientSubscriptionEvent.InvokeAsync(subscriptionReceivedEventArgs).ConfigureAwait(false);
            }

            return subscriptionReceivedEventArgs;
        }

        async Task<InterceptingMqttClientUnsubscriptionEventArgs> InterceptUnsubscribe(string topicFilter, MqttSubscription mqttSubscription, CancellationToken cancellationToken)
        {
            var clientUnsubscribingTopicEventArgs = new InterceptingMqttClientUnsubscriptionEventArgs
            {
                ClientId = _clientSession.Id,
                Topic = topicFilter,
                SessionItems = _clientSession.Items,
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

            await _eventContainer.InterceptingClientUnsubscriptionEvent.InvokeAsync(clientUnsubscribingTopicEventArgs).ConfigureAwait(false);

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