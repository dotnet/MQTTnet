using System.Collections.Generic;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttUnsubscribeResponse
    {
        /// <summary>
        /// Gets or sets the reason code which is sent to the client.
        /// MQTTv5 only.
        /// </summary>
        public MqttUnsubscribeReasonCode ReasonCode { get; set; }
        
        public List<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
        
        public string ReasonString { get; set; }
    }
}