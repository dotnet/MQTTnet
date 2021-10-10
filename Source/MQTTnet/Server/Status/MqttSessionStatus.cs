using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Implementations;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server.Status
{
    public sealed class MqttSessionStatus : IMqttSessionStatus
    {
        readonly MqttClientSession _session;
        readonly MqttClientSessionsManager _sessionsManager;

        public MqttSessionStatus(MqttClientSession session, MqttClientSessionsManager sessionsManager)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));

            session.Deleted += (_, __) => Deleted?.Invoke(this, EventArgs.Empty);
        }
        
        public event EventHandler Deleted;
        
        public string ClientId { get; set; }

        public long PendingApplicationMessagesCount { get; set; }

        public DateTime CreatedTimestamp { get; set; }
        
        public IDictionary<object, object> Items { get; set; }
        
        public Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            
            _session.ApplicationMessagesQueue.Enqueue(new MqttQueuedApplicationMessage
            {
                ApplicationMessage = applicationMessage,
                IsDuplicate = false,
                IsRetainedMessage = false,
                SubscriptionQualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                SenderClientId = null
            });

            return PlatformAbstractionLayer.CompletedTask;
        }

        public Task ClearApplicationMessagesQueueAsync()
        {
            _session.ApplicationMessagesQueue.Clear();
            return Task.FromResult(0);
        }

        public Task DeleteAsync()
        {
            return _sessionsManager.DeleteSessionAsync(ClientId);
        }
        
        public Task ClearPendingApplicationMessagesAsync()
        {
            return ClearApplicationMessagesQueueAsync();
        }
    }
}
