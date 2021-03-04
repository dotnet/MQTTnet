using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        }

        /// <summary>
        /// Gets or sets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; set; }

        public long PendingApplicationMessagesCount { get; set; }

        public DateTime CreatedTimestamp { get; set; }

        public IDictionary<object, object> SessionItems { get; set; }

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
