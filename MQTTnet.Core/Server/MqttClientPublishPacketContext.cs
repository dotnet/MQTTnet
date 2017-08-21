using System;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientPublishPacketContext
    {
        public MqttClientPublishPacketContext(MqttPublishPacket publishPacket)
        {
            PublishPacket = publishPacket ?? throw new ArgumentNullException(nameof(publishPacket));
        }

        public MqttPublishPacket PublishPacket { get; }

        public int SendTries { get; set; }

        public bool IsSent { get; set; }
    }
}
