// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientStatus
    {
        readonly MqttClient _client;

        public MqttClientStatus(MqttClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Gets or sets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string Id => _client.Id;

        public string Endpoint => _client.Endpoint;

        public MqttProtocolVersion ProtocolVersion => _client.ChannelAdapter.PacketFormatterAdapter.ProtocolVersion;

        public DateTime ConnectedTimestamp => _client.Statistics.ConnectedTimestamp;

        public DateTime LastPacketReceivedTimestamp => _client.Statistics.LastPacketReceivedTimestamp;

        public DateTime LastPacketSentTimestamp => _client.Statistics.LastPacketSentTimestamp;

        public DateTime LastNonKeepAlivePacketReceivedTimestamp => _client.Statistics.LastNonKeepAlivePacketReceivedTimestamp;

        public long ReceivedApplicationMessagesCount => _client.Statistics.ReceivedApplicationMessagesCount;

        public long SentApplicationMessagesCount => _client.Statistics.SentApplicationMessagesCount;

        public long ReceivedPacketsCount => _client.Statistics.ReceivedPacketsCount;

        public long SentPacketsCount => _client.Statistics.SentPacketsCount;

        public MqttSessionStatus Session { get; set; }

        public long BytesSent => _client.ChannelAdapter.BytesSent;

        public long BytesReceived => _client.ChannelAdapter.BytesReceived;
        
        public Task DisconnectAsync()
        {
            return _client.StopAsync(MqttDisconnectReasonCode.NormalDisconnection);
        }

        public void ResetStatistics()
        {
            _client.ResetStatistics();
        }
    }
}
