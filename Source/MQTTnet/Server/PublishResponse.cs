using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class PublishResponse
    {
        public MqttPubAckReasonCode ReasonCode { get; set; } = MqttPubAckReasonCode.Success;
        
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
    }
}