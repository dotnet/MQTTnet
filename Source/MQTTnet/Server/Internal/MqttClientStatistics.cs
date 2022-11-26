// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class MqttClientStatistics
    {
        Statistics _statistics = new Statistics     // mutable struct, don't make readonly!
        {
            // Start with 1 because the CONNACK packet is not counted here.
            _receivedPacketsCount = 1,

            // Start with 1 because the CONNECT packet is not counted here.
            _sentPacketsCount = 1
        };

        public MqttClientStatistics()
        {
            ConnectedTimestamp = DateTime.UtcNow;

            LastPacketReceivedTimestamp = ConnectedTimestamp;
            LastPacketSentTimestamp = ConnectedTimestamp;

            LastNonKeepAlivePacketReceivedTimestamp = ConnectedTimestamp;
        }

        public DateTime ConnectedTimestamp { get; }

        /// <summary>
        ///     Timestamp of the last package that has been sent to the client ("received" from the client's perspective)
        /// </summary>
        public DateTime LastPacketReceivedTimestamp { get; private set; }

        /// <summary>
        ///     Timestamp of the last package that has been received from the client ("sent" from the client's perspective)
        /// </summary>
        public DateTime LastPacketSentTimestamp { get; private set; }

        public DateTime LastNonKeepAlivePacketReceivedTimestamp { get; private set; }

        public long SentApplicationMessagesCount => Volatile.Read(ref _statistics._sentApplicationMessagesCount);

        public long ReceivedApplicationMessagesCount => Volatile.Read(ref _statistics._receivedApplicationMessagesCount);

        public long SentPacketsCount => Volatile.Read(ref _statistics._sentPacketsCount);

        public long ReceivedPacketsCount => Volatile.Read(ref _statistics._receivedPacketsCount);

        public void HandleReceivedPacket(MqttPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            // This class is tracking all values from Clients perspective!
            LastPacketSentTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _statistics._sentPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _statistics._sentApplicationMessagesCount);
            }

            if (!(packet is MqttPingReqPacket || packet is MqttPingRespPacket))
            {
                LastNonKeepAlivePacketReceivedTimestamp = LastPacketReceivedTimestamp;
            }
        }
        public void ResetStatistics() => _statistics.Reset();

        public void HandleSentPacket(MqttPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            // This class is tracking all values from Clients perspective!
            LastPacketReceivedTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _statistics._receivedPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _statistics._receivedApplicationMessagesCount);
            }
        }

        private struct Statistics
        {
            public long _receivedPacketsCount;
            public long _sentPacketsCount;

            public long _receivedApplicationMessagesCount;
            public long _sentApplicationMessagesCount;

            public void Reset()
            {
                Volatile.Write(ref _receivedPacketsCount, 0);
                Volatile.Write(ref _sentPacketsCount, 0);
                Volatile.Write(ref _receivedApplicationMessagesCount, 0);
                Volatile.Write(ref _sentApplicationMessagesCount, 0);
            }
        }
    }
}