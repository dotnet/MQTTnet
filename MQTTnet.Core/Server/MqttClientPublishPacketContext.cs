using System;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientPublishPacketContext
    {
        public MqttClientPublishPacketContext(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            SenderClientSession = senderClientSession ?? throw new ArgumentNullException(nameof(senderClientSession));
            PublishPacket = publishPacket ?? throw new ArgumentNullException(nameof(publishPacket));
        }

        public MqttClientSession SenderClientSession { get; }

        public MqttPublishPacket PublishPacket { get; }

        public int SendTries { get; set; }

        public bool IsSent { get; set; }
    }
}
