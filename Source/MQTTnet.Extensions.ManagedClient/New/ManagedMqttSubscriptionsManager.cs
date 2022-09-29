// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Protocol;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttSubscriptionsManager
    {
        readonly MqttNetSourceLogger _logger;
        readonly IMqttClient _mqttClient;
        
        // Do not use a dictionary in order to preserve subscription ordering (retained messages).
        readonly List<ManagedMqttSubscription> _subscriptions = new List<ManagedMqttSubscription>();

        public ManagedMqttSubscriptionsManager(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource<ManagedMqttSubscriptionsManager>();
        }

        public AsyncEvent<SubscribeProcessedEventArgs> SubscribeProcessedEvent { get; } = new AsyncEvent<SubscribeProcessedEventArgs>();

        public AsyncEvent<UnsubscribeProcessedEventArgs> UnsubscribeProcessedEvent { get; } = new AsyncEvent<UnsubscribeProcessedEventArgs>();

        public void ResubscribeAll()
        {
            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.IsPending = true;
                }
            }
        }

        public void Subscribe(MqttClientSubscribeOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.TopicFilters.Count > 1)
            {
                throw new NotSupportedException("The managed client only supports subscribing to one topic at the same time.");
            }

            var topic = options.TopicFilters.First().Topic;
            
            lock (_subscriptions)
            {
                MqttTopicValidator.ThrowIfInvalidSubscribe(topic);

                _subscriptions.RemoveAll(s => s.Topic.Equals(topic));
                _subscriptions.Add(new ManagedMqttSubscription(options));
            }
        }

        public async Task Synchronize(CancellationToken cancellationToken)
        {
            // TODO: Remember real changes.

            List<ManagedMqttSubscription> pendingSubscriptions;
            List<ManagedMqttSubscription> obsoleteSubscriptions;
            lock (_subscriptions)
            {
                pendingSubscriptions = _subscriptions.Where(s => s.IsPending).ToList();
                obsoleteSubscriptions = _subscriptions.Where(s => s.IsObsolete).ToList();
            }

            if (pendingSubscriptions.Count == 0 && obsoleteSubscriptions.Count == 0)
            {
                _logger.Verbose("No subscriptions to synchronize");
                return;
            }

            foreach (var subscription in pendingSubscriptions)
            {
                var subscribeResult = await _mqttClient.SubscribeAsync(subscription.SubscribeOptions, cancellationToken).ConfigureAwait(false);
                subscription.IsPending = false;
                await FireSubscribeProcessedEvent(subscription, subscribeResult).ConfigureAwait(false);
            }

            foreach (var subscription in obsoleteSubscriptions)
            {
                var unsubscribeResult = await _mqttClient.UnsubscribeAsync(subscription.UnsubscribeOptions, cancellationToken).ConfigureAwait(false);
                lock (_subscriptions)
                {
                    _subscriptions.Remove(subscription);
                }

                await FireUnsubscribeProcessedEvent(subscription, unsubscribeResult).ConfigureAwait(false);
            }
        }

        public void Unsubscribe(MqttClientUnsubscribeOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (_subscriptions)
            {
                var topic = options.TopicFilters.First();
                var existingSubscription = _subscriptions.FirstOrDefault(s => s.Topic.Equals(topic));

                if (existingSubscription != null)
                {
                    existingSubscription.MarkAsUnsubscribed(options);
                }
            }
        }

        async Task FireSubscribeProcessedEvent(ManagedMqttSubscription subscription, MqttClientSubscribeResult subscribeResult)
        {
            _logger.Info("Subscribed topic '{0}'.", subscription.Topic);

            if (SubscribeProcessedEvent.HasHandlers)
            {
                var resultCode = subscribeResult?.Items.First().ResultCode ?? MqttClientSubscribeResultCode.UnspecifiedError;
                var eventArgs = new SubscribeProcessedEventArgs(subscription.SubscribeOptions, resultCode, subscribeResult?.UserProperties);
                await SubscribeProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
        }

        async Task FireUnsubscribeProcessedEvent(ManagedMqttSubscription subscription, MqttClientUnsubscribeResult unsubscribeResult)
        {
            _logger.Info("Unsubscribed topic '{0}'.", subscription.Topic);

            if (UnsubscribeProcessedEvent.HasHandlers)
            {
                var resultCode = unsubscribeResult?.Items.First().ResultCode ?? MqttClientUnsubscribeResultCode.UnspecifiedError;
                var eventArgs = new UnsubscribeProcessedEventArgs(subscription.UnsubscribeOptions, resultCode, unsubscribeResult?.UserProperties);
                await UnsubscribeProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
        }
    }
}