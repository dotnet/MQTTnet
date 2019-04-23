
using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Publishing
{
    public class MqttClientPublishResult
    {
        public ushort? PacketIdentifier { get; set; }

        public MqttClientPublishReasonCode ReasonCode { get; set; } = MqttClientPublishReasonCode.Success;

        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
