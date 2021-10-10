using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <summary>
        /// Fired when this session is being deleted.
        /// </summary>
        public event EventHandler Deleted;
        
        /// <summary>
        /// Gets or sets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; set; }

        public long PendingApplicationMessagesCount { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        /// <summary>
        /// This items can be used by the library user in order to store custom information.
        /// </summary>
        public IDictionary<object, object> Items { get; set; }
        
        public Task DeleteAsync()
        {
            return _sessionsManager.DeleteSessionAsync(ClientId);
        }

        public Task ClearPendingApplicationMessagesAsync()
        {
            _session.ApplicationMessagesQueue.Clear();
            return Task.FromResult(0);
        }
    }
}
