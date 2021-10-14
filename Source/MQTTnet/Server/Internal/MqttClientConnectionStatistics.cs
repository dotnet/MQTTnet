using System;
using System.Threading;
using MQTTnet.Packets;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientConnectionStatistics
    {
        // Start with 1 because the CONNACK packet is not counted here.
        long _receivedPacketsCount = 1;

        // Start with 1 because the CONNECT packet is not counted here.
        long _sentPacketsCount = 1;

        long _receivedApplicationMessagesCount;
        long _sentApplicationMessagesCount;

        public MqttClientConnectionStatistics()
        {
            ConnectedTimestamp = DateTime.UtcNow;

            LastPacketReceivedTimestamp = ConnectedTimestamp;
            LastPacketSentTimestamp = ConnectedTimestamp;

            LastNonKeepAlivePacketReceivedTimestamp = ConnectedTimestamp;
        }

        public DateTime ConnectedTimestamp { get; }

        public DateTime LastPacketReceivedTimestamp { get; private set; }

        public DateTime LastPacketSentTimestamp { get; private set; }
        
        public DateTime LastNonKeepAlivePacketReceivedTimestamp { get; private set; }

        public long SentApplicationMessagesCount => Interlocked.Read(ref _sentApplicationMessagesCount);
        
        public long ReceivedApplicationMessagesCount => Interlocked.Read(ref _receivedApplicationMessagesCount);
        
        public long SentPacketsCount => Interlocked.Read(ref _sentPacketsCount);
        
        public long ReceivedPacketsCount => Interlocked.Read(ref _receivedPacketsCount);
        
        public void HandleReceivedPacket(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
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

        public void HandleSentPacket(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
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