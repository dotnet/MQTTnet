﻿using MQTTnet.Formatter;
using System;
using System.Threading.Tasks;
using MQTTnet.Client.Disconnecting;

namespace MQTTnet.Server.Status
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
            return _connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection);
        }

        public void ResetStatistics()
        {
            _connection.ResetStatistics();
        }
    }
}
