using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public sealed class MqttApplicationMessageResponse
    {
        public MqttApplicationMessageResponseReasonCode ReasonCode { get; set; } = MqttApplicationMessageResponseReasonCode.Success;
        
        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
    }
}