using System;
using System.Threading.Tasks;
using MQTTnet.Serializer;

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
        public int PendingApplicationMessagesCount { get; set; }

        public Task DisconnectAsync()
        {
            _session.Stop(MqttClientDisconnectType.NotClean);
            return Task.FromResult(0);
        }

        public Task DeleteSessionAsync()
        {
            try
            {
                _session.Stop(MqttClientDisconnectType.NotClean);
            }
            finally
            {
                _sessionsManager.DeleteSession(ClientId);
            }

            return Task.FromResult(0);
        }

        public Task ClearPendingApplicationMessagesAsync()
        {
            _session.ClearPendingApplicationMessages();

            return Task.FromResult(0);
        }
    }
}
