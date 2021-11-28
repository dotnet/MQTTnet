using System;
using System.Collections.Generic;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSession : IDisposable
    {
        readonly DateTime _createdTimestamp = DateTime.UtcNow;

        public MqttClientSession(
            string clientId,
            IDictionary<object, object> items,
            MqttServerEventDispatcher eventDispatcher,
            IMqttServerOptions serverOptions,
            IMqttRetainedMessagesManager retainedMessagesManager,
            bool isPersistent
            )
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Items = items ?? throw new ArgumentNullException(nameof(items));
            IsPersistent = isPersistent;
            SubscriptionsManager = new MqttClientSubscriptionsManager(this, serverOptions, eventDispatcher, retainedMessagesManager);
            ApplicationMessagesQueue = new MqttClientSessionApplicationMessagesQueue(serverOptions);
        }

        public string ClientId { get; }

        public bool IsCleanSession { get; set; } = true;

        /// <summary>
        /// Session should persist if CleanSession was set to false (Mqtt3) or if SessionExpiryInterval != 0 (Mqtt5)
        /// </summary>
        public bool IsPersistent { get; set; }

        public MqttApplicationMessage WillMessage { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }

        public MqttClientSessionApplicationMessagesQueue ApplicationMessagesQueue { get; }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this session.
        /// </summary>
        public IDictionary<object, object> Items { get; }
        
        public void FillSessionStatus(MqttSessionStatus status)
        {
            status.ClientId = ClientId;
            status.CreatedTimestamp = _createdTimestamp;
            status.PendingApplicationMessagesCount = ApplicationMessagesQueue.Count;
            status.Items = Items;
        }

        public void Dispose()
        {
            ApplicationMessagesQueue?.Dispose();
        }
    }
}