using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSession
    {
        readonly MqttClientSessionsManager _clientSessionsManager;

        public MqttClientSession(string clientId,
            IDictionary<object, object> items,
            MqttServerEventDispatcher eventDispatcher,
            IMqttServerOptions serverOptions,
            IMqttRetainedMessagesManager retainedMessagesManager,
            MqttClientSessionsManager clientSessionsManager)
        {
            _clientSessionsManager = clientSessionsManager ?? throw new ArgumentNullException(nameof(clientSessionsManager));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Items = items ?? throw new ArgumentNullException(nameof(items));

            SubscriptionsManager = new MqttClientSubscriptionsManager(this, serverOptions, eventDispatcher, retainedMessagesManager);
            ApplicationMessagesQueue = new MqttClientSessionApplicationMessagesQueue(serverOptions);
        }

        public event EventHandler Deleted;
        
        public string ClientId { get; }

        public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;
        
        public bool IsCleanSession { get; set; } = true;

        public MqttApplicationMessage WillMessage { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }
        
        public MqttClientSessionApplicationMessagesQueue ApplicationMessagesQueue { get; }

        public IDictionary<object, object> Items { get; }
        
        public Task DeleteAsync()
        {
            return _clientSessionsManager.DeleteSessionAsync(ClientId);
        }
        
        public void OnDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }
    }
}