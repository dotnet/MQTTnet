using System;
using System.Threading.Tasks;

namespace MQTTnet.Server.Status
{
    public class MqttSessionStatus : IMqttSessionStatus
    {
        private readonly MqttClientSession _session;
        private readonly MqttClientSessionsManager _sessionsManager;

        public MqttSessionStatus(MqttClientSession session, MqttClientSessionsManager sessionsManager)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
        }

        public string ClientId { get; set; }

        public long PendingApplicationMessagesCount { get; set; }

        public bool IsConnected { get; set; }
    
        public DateTime CreatedTimestamp { get; set; }

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
