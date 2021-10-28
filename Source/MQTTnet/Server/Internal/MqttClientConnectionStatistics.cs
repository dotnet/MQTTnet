using System;
using System.Threading;
using MQTTnet.Packets;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientConnectionStatistics
    {
        readonly DateTime _connectedTimestamp;

        DateTime _lastNonKeepAlivePacketReceivedTimestamp;
        DateTime _lastPacketReceivedTimestamp;
        DateTime _lastPacketSentTimestamp;

        // Start with 1 because the CONNACK packet is not counted here.
        long _receivedPacketsCount = 1;

        // Start with 1 because the CONNECT packet is not counted here.
        long _sentPacketsCount = 1;

        long _receivedApplicationMessagesCount;
        long _sentApplicationMessagesCount;

        public MqttClientConnectionStatistics()
        {
            _connectedTimestamp = DateTime.UtcNow;

            _lastPacketReceivedTimestamp = _connectedTimestamp;
            _lastPacketSentTimestamp = _connectedTimestamp;

            _lastNonKeepAlivePacketReceivedTimestamp = _connectedTimestamp;
        }

        // This class is tracking all values from the Client's perspective regarding the internal "received"/"sent" timestamps.
        // "Sent" means, a package has been received from the client.
        // "Received" means, a package has been sent by the server.
        // The public property below makes it less ambiguous.

        public DateTime LastPacketReceivedFromClientTimestamp => _lastPacketSentTimestamp;


        public void HandleReceivedPacket(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
            // This class is tracking all values from Clients perspective!
            _lastPacketSentTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _sentPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _sentApplicationMessagesCount);
            }

            if (!(packet is MqttPingReqPacket || packet is MqttPingRespPacket))
            {
                _lastNonKeepAlivePacketReceivedTimestamp = _lastPacketReceivedTimestamp;
            }
        }

        public void HandleSentPacket(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
            // This class is tracking all values from Clients perspective!
            _lastPacketReceivedTimestamp = DateTime.UtcNow;

            Interlocked.Increment(ref _receivedPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _receivedApplicationMessagesCount);
            }
        }

        public void FillClientStatus(MqttClientStatus clientStatus)
        {
            if (clientStatus == null) throw new ArgumentNullException(nameof(clientStatus));
            
            clientStatus.ConnectedTimestamp = _connectedTimestamp;

            clientStatus.ReceivedPacketsCount = Interlocked.Read(ref _receivedPacketsCount);
            clientStatus.SentPacketsCount = Interlocked.Read(ref _sentPacketsCount);

            clientStatus.ReceivedApplicationMessagesCount = Interlocked.Read(ref _receivedApplicationMessagesCount);
            clientStatus.SentApplicationMessagesCount = Interlocked.Read(ref _sentApplicationMessagesCount);

            clientStatus.LastPacketReceivedTimestamp = _lastPacketReceivedTimestamp;
            clientStatus.LastPacketSentTimestamp = _lastPacketSentTimestamp;

            clientStatus.LastNonKeepAlivePacketReceivedTimestamp = _lastNonKeepAlivePacketReceivedTimestamp;
        }
    }
}