using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public class MqttClientSessionStatus : IMqttClientSessionStatus
    {
        private readonly MqttClientSessionsManager _sessionsManager;
        private readonly MqttClientSession _session;

        public MqttClientSessionStatus(MqttClientSessionsManager sessionsManager, MqttClientSession session)
        {
            _sessionsManager = sessionsManager;
            _session = session;
        }

        public string ClientId { get; set; }
        public string Endpoint { get; set; }
        public bool IsConnected { get; set; }
        public MqttProtocolVersion? ProtocolVersion { get; set; }
        public TimeSpan LastPacketReceived { get; set; }
        public TimeSpan LastNonKeepAlivePacketReceived { get; set; }
        public long PendingApplicationMessagesCount { get; set; }
        public long ReceivedApplicationMessagesCount { get; set; }
        public long SentApplicationMessagesCount { get; set; }

        public Task DisconnectAsync()
        {
            return _session.StopAsync(MqttClientDisconnectType.NotClean);
        }

        public async Task DeleteSessionAsync()
        {
            try
            {
                await _session.StopAsync(MqttClientDisconnectType.NotClean).ConfigureAwait(false);
            }
            finally
            {
                await _sessionsManager.DeleteSessionAsync(ClientId).ConfigureAwait(false);
            }
        }

        public Task ClearPendingApplicationMessagesAsync()
        {
            _session.ClearPendingApplicationMessages();

            return Task.FromResult(0);
        }
    }
}
