using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.Server.Status
{
    public class MqttClientStatus : IMqttClientStatus
    {
        private readonly MqttClientSessionsManager _sessionsManager;
        private readonly MqttClientConnection _connection;

        public MqttClientStatus(MqttClientConnection connection, MqttClientSessionsManager sessionsManager)
        {
            _connection = connection;
            _sessionsManager = sessionsManager;
        }

        public string ClientId { get; set; }

        public string Endpoint { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }

        public DateTime LastPacketReceivedTimestamp { get; set; }

        public DateTime LastNonKeepAlivePacketReceivedTimestamp { get; set; }

        public long ReceivedApplicationMessagesCount { get; set; }

        public long SentApplicationMessagesCount { get; set; }

        public long ReceivedPacketsCount { get; set; }

        public long SentPacketsCount { get; set; }

        public IMqttSessionStatus Session { get; set; }

        public Task DisconnectAsync()
        {
            return _connection.StopAsync();
        }
    }
}
