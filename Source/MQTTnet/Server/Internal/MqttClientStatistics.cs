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
        // Start with 1 because the CONNACK packet is not counted here.
        long _receivedPacketsCount = 1;

        // Start with 1 because the CONNECT packet is not counted here.
        long _sentPacketsCount = 1;

        long _receivedApplicationMessagesCount;
        long _sentApplicationMessagesCount;

        public MqttClientStatistics()
        {
            ConnectedTimestamp = DateTime.UtcNow;

            LastPacketReceivedTimestamp = ConnectedTimestamp;
            LastPacketSentTimestamp = ConnectedTimestamp;

            LastNonKeepAlivePacketReceivedTimestamp = ConnectedTimestamp;
        }
        
        public DateTime ConnectedTimestamp { get; }

        /// <summary>
        /// Timestamp of the last package that has been sent to the client ("received" from the client's perspective)
        /// </summary>
        public DateTime LastPacketReceivedTimestamp { get; private set; }
                
        /// <summary>
        /// Timestamp of the last package that has been received from the client ("sent" from the client's perspective)
        /// </summary>
        public DateTime LastPacketSentTimestamp { get; private set; }
        
        public DateTime LastNonKeepAlivePacketReceivedTimestamp { get; private set; }

        public long SentApplicationMessagesCount => Interlocked.Read(ref _sentApplicationMessagesCount);
        
        public long ReceivedApplicationMessagesCount => Interlocked.Read(ref _receivedApplicationMessagesCount);
        
        public long SentPacketsCount => Interlocked.Read(ref _sentPacketsCount);
        
        public long ReceivedPacketsCount => Interlocked.Read(ref _receivedPacketsCount);
        
        public void HandleReceivedPacket(MqttPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }
            
            // This class is tracking all values from Clients perspective!
            LastPacketSentTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _sentPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _sentApplicationMessagesCount);
            }

            if (!(packet is MqttPingReqPacket || packet is MqttPingRespPacket))
            {
                LastNonKeepAlivePacketReceivedTimestamp = LastPacketReceivedTimestamp;
            }
        }
        public void ResetStatistics(){
            Interlocked.Exchange(ref _sentApplicationMessagesCount,0);
            Interlocked.Exchange(ref _receivedApplicationMessagesCount,0);
            Interlocked.Exchange(ref _sentPacketsCount,0);
            Interlocked.Exchange(ref _receivedPacketsCount,0);
        }
        
        public void HandleSentPacket(MqttPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }
            
            // This class is tracking all values from Clients perspective!
            LastPacketReceivedTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _receivedPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _receivedApplicationMessagesCount);
            }
        }
    }
}