﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Server.Status;

namespace MQTTnet.Server
{
    public class MqttClientSession
    {
        private readonly DateTime _createdTimestamp = DateTime.UtcNow;

        public MqttClientSession(string clientId, MqttServerEventDispatcher eventDispatcher, IMqttServerOptions serverOptions)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));

            SubscriptionsManager = new MqttClientSubscriptionsManager(clientId, eventDispatcher, serverOptions);
            ApplicationMessagesQueue = new MqttClientSessionApplicationMessagesQueue(serverOptions);
        }

        public string ClientId { get; }

        public bool IsCleanSession { get; set; } = true;

        public MqttApplicationMessage WillMessage { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }

        public MqttClientSessionApplicationMessagesQueue ApplicationMessagesQueue { get; }

        public void EnqueueApplicationMessage(MqttApplicationMessage applicationMessage, string senderClientId, bool isRetainedApplicationMessage)
        {
            var checkSubscriptionsResult = SubscriptionsManager.CheckSubscriptions(applicationMessage.Topic, applicationMessage.QualityOfServiceLevel);
            if (!checkSubscriptionsResult.IsSubscribed)
            {
                return;
            }

            ApplicationMessagesQueue.Enqueue(applicationMessage, senderClientId, checkSubscriptionsResult.QualityOfServiceLevel, isRetainedApplicationMessage);
        }

        public async Task SubscribeAsync(ICollection<TopicFilter> topicFilters, MqttRetainedMessagesManager retainedMessagesManager)
        {
            await SubscriptionsManager.SubscribeAsync(topicFilters).ConfigureAwait(false);
            var matchingRetainedMessages = await retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var matchingRetainedMessage in matchingRetainedMessages)
            {
                EnqueueApplicationMessage(matchingRetainedMessage, null, true);
            }
        }

        public Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            return SubscriptionsManager.UnsubscribeAsync(topicFilters);
        }

        public void FillStatus(MqttSessionStatus status)
        {
            status.ClientId = ClientId;
            status.CreatedTimestamp = _createdTimestamp;
            status.PendingApplicationMessagesCount = ApplicationMessagesQueue.Count;
        }
    }
}