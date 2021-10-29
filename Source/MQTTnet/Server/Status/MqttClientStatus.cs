using System;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public sealed class MqttClientStatus : IMqttClientStatus
    {
        readonly MqttClientConnection _connection;

        public MqttClientStatus(MqttClientConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Gets or sets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId => _connection.Id;

        public string Endpoint => _connection.Endpoint;

        public MqttProtocolVersion ProtocolVersion => _connection.ChannelAdapter.PacketFormatterAdapter.ProtocolVersion;

        public DateTime ConnectedTimestamp => _connection.Statistics.ConnectedTimestamp;

        public DateTime LastPacketReceivedTimestamp => _connection.Statistics.LastPacketReceivedTimestamp;

        public DateTime LastPacketSentTimestamp => _connection.Statistics.LastPacketSentTimestamp;

        public DateTime LastNonKeepAlivePacketReceivedTimestamp => _connection.Statistics.LastNonKeepAlivePacketReceivedTimestamp;

        public long ReceivedApplicationMessagesCount => _connection.Statistics.ReceivedApplicationMessagesCount;

        public long SentApplicationMessagesCount => _connection.Statistics.SentApplicationMessagesCount;

        public long ReceivedPacketsCount => _connection.Statistics.ReceivedPacketsCount;

        public long SentPacketsCount => _connection.Statistics.SentPacketsCount;

        public IMqttSessionStatus Session { get; set; }

        public long BytesSent => _connection.ChannelAdapter.BytesSent;

        public long BytesReceived => _connection.ChannelAdapter.BytesReceived;
        
        public Task DisconnectAsync()
        {
            return _connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection);
        }

        public void ResetStatistics()
        {
            _connection.ResetStatistics();
        }
    }
}
