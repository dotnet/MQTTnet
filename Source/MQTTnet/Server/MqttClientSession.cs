using MQTTnet.Diagnostics;
using MQTTnet.Server.Status;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttClientSession
    {
        readonly IMqttNetScopedLogger _logger;

        readonly DateTime _createdTimestamp = DateTime.UtcNow;
        readonly IMqttRetainedMessagesManager _retainedMessagesManager;

        public MqttClientSession(
            string clientId,
            IDictionary<object, object> items,
            MqttServerEventDispatcher eventDispatcher,
            IMqttServerOptions serverOptions, 
            IMqttRetainedMessagesManager retainedMessagesManager,
            IMqttNetLogger logger)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Items = items ?? throw new ArgumentNullException(nameof(items));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            SubscriptionsManager = new MqttClientSubscriptionsManager(this, eventDispatcher, serverOptions);
            ApplicationMessagesQueue = new MqttClientSessionApplicationMessagesQueue(serverOptions);

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttClientSession));
        }

        public string ClientId { get; }

        public bool IsCleanSession { get; set; } = true;

        public MqttApplicationMessage WillMessage { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }

        public MqttClientSessionApplicationMessagesQueue ApplicationMessagesQueue { get; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> Items { get; }

        public bool EnqueueApplicationMessage(MqttApplicationMessage applicationMessage, string senderClientId, bool isRetainedApplicationMessage)
        {
            var checkSubscriptionsResult = SubscriptionsManager.CheckSubscriptions(applicationMessage.Topic, applicationMessage.QualityOfServiceLevel);
            if (!checkSubscriptionsResult.IsSubscribed)
            {
                return false;
            }

            _logger.Verbose("Client '{0}': Queued application message with topic '{1}'.", ClientId, applicationMessage.Topic);

            ApplicationMessagesQueue.Enqueue(applicationMessage, senderClientId, checkSubscriptionsResult.QualityOfServiceLevel, isRetainedApplicationMessage);

            return true;
        }

        public async Task SubscribeAsync(ICollection<MqttTopicFilter> topicFilters)
        {
            if (topicFilters is null) throw new ArgumentNullException(nameof(topicFilters));

            await SubscriptionsManager.SubscribeAsync(topicFilters).ConfigureAwait(false);

            var matchingRetainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var matchingRetainedMessage in matchingRetainedMessages)
            {
                EnqueueApplicationMessage(matchingRetainedMessage, null, true);
            }
        }

        public Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            if (topicFilters is null) throw new ArgumentNullException(nameof(topicFilters));

            return SubscriptionsManager.UnsubscribeAsync(topicFilters);
        }

        public void FillStatus(MqttSessionStatus status)
        {
            status.ClientId = ClientId;
            status.CreatedTimestamp = _createdTimestamp;
            status.PendingApplicationMessagesCount = ApplicationMessagesQueue.Count;
            status.Items = Items;
        }
    }
}