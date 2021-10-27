﻿using System;
using System.Threading.Tasks;
using MQTTnet.Formatter;

namespace MQTTnet.Server
{
    public interface IMqttClientStatus
    {
        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        string ClientId { get; }

        string Endpoint { get; }

        MqttProtocolVersion ProtocolVersion { get; }

        DateTime ConnectedTimestamp { get; }
        
        DateTime LastPacketReceivedTimestamp { get; }

        DateTime LastPacketSentTimestamp { get; }
        
        DateTime LastNonKeepAlivePacketReceivedTimestamp { get; }

        long ReceivedApplicationMessagesCount { get; }

        long SentApplicationMessagesCount { get; }

        long ReceivedPacketsCount { get; }

        long SentPacketsCount { get; }

        IMqttSessionStatus Session { get; }

        long BytesSent { get; }

        long BytesReceived { get; }

        Task DisconnectAsync();

        void ResetStatistics();
    }
}
