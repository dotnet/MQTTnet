using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.Server.Status
{
    public class MqttClientStatus : IMqttClientStatus
    {
        private readonly MqttClientConnection _connection;

        public MqttClientStatus(MqttClientConnection connection)
        {
            _connection = connection;
        }

        public string ClientId { get; set; }

        public string Endpoint { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }

        public DateTime LastPacketReceivedTimestamp { get; set; }

        public DateTime ConnectedTimestamp { get; set; }

        public DateTime LastNonKeepAlivePacketReceivedTimestamp { get; set; }

        public long ReceivedApplicationMessagesCount { get; set; }

        public long SentApplicationMessagesCount { get; set; }

        public long ReceivedPacketsCount { get; set; }

        public long SentPacketsCount { get; set; }

        public IMqttSessionStatus Session { get; set; }

        public long BytesSent { get; set; }

        public long BytesReceived { get; set; }

        public Task DisconnectAsync()
        {
            return _connection.StopAsync();
        }

        public void ResetStatistics()
        {
            _connection.ResetStatistics();
        }
    }
}
